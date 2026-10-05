using System.Threading.Channels;
using SparkRaftSharp.Configuration;
using SparkRaftSharp.Logging;
using SparkRaftSharp.Messaging;
using SparkRaftSharp.Models;
using SparkRaftSharp.Observers;
using SparkRaftSharp.Persistence;
using SparkRaftSharp.Primitives;
using SparkRaftSharp.StateMachine;
using SparkRaftSharp.Time;
using SparkRaftSharp.Transport;
using SparkRaftSharp.Internal.Services;

namespace SparkRaftSharp.Internal;

internal sealed class RaftEngine : IAsyncDisposable
{
    private readonly RaftRuntime _runtime;
    private readonly RaftTimerScheduler _timers;
    private readonly RaftObserverNotifier _notifier;
    private readonly LogCommitService _commitService;
    private readonly ElectionService _election;
    private readonly ReplicationService _replication;
    private readonly AppendEntriesService _appendEntries;
    private readonly InstallSnapshotService _installSnapshot;
    private readonly Channel<RaftEvent> _events;
    private CancellationTokenSource? _runCts;
    private Task? _loopTask;

    public RaftEngine(
        ClusterConfiguration cluster,
        RaftOptions options,
        ILogStore log,
        IStateStore stateStore,
        ISnapshotStore? snapshotStore,
        IRaftTransport transport,
        IRaftStateMachine stateMachine,
        IRaftClock clock,
        IRaftLogger logger,
        IElectionTimeoutJitter? jitter,
        IEnumerable<IRaftObserver>? observers)
    {
        _notifier = new RaftObserverNotifier(observers?.ToArray() ?? Array.Empty<IRaftObserver>());
        _runtime = new RaftRuntime(
            cluster, options, log, stateStore, snapshotStore, transport, stateMachine, logger, _notifier);

        _timers = new RaftTimerScheduler(clock, options, jitter ?? new RandomElectionTimeoutJitter());
        _commitService = new LogCommitService(_runtime, _notifier);
        _replication = new ReplicationService(_runtime, _commitService);
        _appendEntries = new AppendEntriesService(_runtime, _commitService);
        _installSnapshot = new InstallSnapshotService(_runtime, _commitService);
        _election = new ElectionService(_runtime, _notifier, BecomeLeader);

        _events = Channel.CreateUnbounded<RaftEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public NodeId NodeId => _runtime.Cluster.LocalNodeId;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _runtime.State = _runtime.StateStore.Load();
        _runtime.Transport.Register(_runtime.Cluster.LocalNodeId, OnTransportMessageAsync);
        _runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loopTask = Task.Run(() => RunLoopAsync(_runCts.Token), CancellationToken.None);
        ScheduleElectionTimeout();
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_runCts is not null)
        {
            await _events.Writer.WriteAsync(new ShutdownEvent());
            _runCts.Cancel();
            if (_loopTask is not null)
                await _loopTask.ConfigureAwait(false);
        }

        _timers.Dispose();
    }

    public Task<LogIndex> ProposeAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<LogIndex>(TaskCreationOptions.RunContinuationsAsynchronously);
        cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

        if (!_events.Writer.TryWrite(new ProposeEvent(data.ToArray(), tcs)))
            return Task.FromException<LogIndex>(new InvalidOperationException("Raft engine is not accepting proposals."));

        return tcs.Task;
    }

    public Task ResignLeadershipAsync(CancellationToken cancellationToken) =>
        _events.Writer.WriteAsync(new ResignLeadershipEvent(), cancellationToken).AsTask();

    public RaftNodeStatus GetStatus() =>
        new(
            _runtime.Role,
            _runtime.State.CurrentTerm,
            _runtime.LeaderId,
            _runtime.State.CommitIndex,
            _runtime.State.LastApplied,
            _runtime.Log.LastIndex);

    private Task OnTransportMessageAsync(IRaftMessage message, CancellationToken cancellationToken) =>
        _events.Writer.WriteAsync(new InboundMessageEvent(message), cancellationToken).AsTask();

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _events.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (_events.Reader.TryRead(out var evt))
                {
                    if (evt is ShutdownEvent)
                        return;

                    await DispatchAsync(evt).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task DispatchAsync(RaftEvent evt)
    {
        switch (evt)
        {
            case ElectionTimeoutEvent:
                _election.OnElectionTimeout();
                ScheduleElectionTimeout();
                break;
            case HeartbeatTickEvent:
                await _replication.ReplicateToAllPeersAsync().ConfigureAwait(false);
                break;
            case InboundMessageEvent inbound:
                await HandleMessageAsync(inbound.Message).ConfigureAwait(false);
                break;
            case ProposeEvent propose:
                await HandleProposeAsync(propose).ConfigureAwait(false);
                break;
            case ResignLeadershipEvent:
                ResignLeadership();
                break;
        }
    }

    private async Task HandleMessageAsync(IRaftMessage message)
    {
        switch (message)
        {
            case RequestVoteRequest rv:
                _election.HandleRequestVote(rv, ScheduleElectionTimeout);
                break;
            case RequestVoteResponse rvRes:
                _election.HandleRequestVoteResponse(rvRes, () => StepDownToFollower(_runtime.State.CurrentTerm, null));
                break;
            case AppendEntriesRequest ae:
                await _appendEntries.HandleAsync(
                    ae,
                    ScheduleElectionTimeout,
                    () => StepDownToFollower(ae.Term, ae.LeaderId)).ConfigureAwait(false);
                break;
            case AppendEntriesResponse aeRes:
                _replication.HandleAppendEntriesResponse(
                    aeRes,
                    () => StepDownToFollower(aeRes.Term, null));
                break;
            case InstallSnapshotRequest snap:
                await _installSnapshot.HandleAsync(
                    snap,
                    ScheduleElectionTimeout,
                    () => StepDownToFollower(snap.Term, snap.LeaderId)).ConfigureAwait(false);
                break;
            case InstallSnapshotResponse snapRes:
                _replication.HandleInstallSnapshotResponse(
                    snapRes,
                    () => StepDownToFollower(snapRes.Term, null));
                break;
        }
    }

    private async Task HandleProposeAsync(ProposeEvent propose)
    {
        if (_runtime.Role != RaftRole.Leader)
        {
            propose.Completion.TrySetException(new Exceptions.NotLeaderException(_runtime.LeaderId));
            return;
        }

        var index = _runtime.Log.LastIndex.Next;
        var entry = new RaftEntry(index, _runtime.State.CurrentTerm, propose.Data);
        _runtime.Log.Append(entry);
        _runtime.PendingProposals[index] = propose.Completion;
        await _replication.ReplicateToAllPeersAsync().ConfigureAwait(false);
    }

    private void BecomeLeader()
    {
        _runtime.Role = RaftRole.Leader;
        _runtime.LeaderId = _runtime.Cluster.LocalNodeId;
        _timers.CancelElection();
        _replication.InitializeLeaderIndices();

        _runtime.Logger.Info(
            $"Node {_runtime.Cluster.LocalNodeId} became leader for term {_runtime.State.CurrentTerm.Value}");
        _notifier.RoleChanged(_runtime.Role, _runtime.State.CurrentTerm);
        _notifier.LeaderChanged(_runtime.LeaderId, _runtime.State.CurrentTerm);
        StartHeartbeatTimer();
        _ = _replication.ReplicateToAllPeersAsync();
    }

    private void ResignLeadership()
    {
        if (_runtime.Role != RaftRole.Leader)
            return;

        _runtime.Role = RaftRole.Follower;
        _runtime.LeaderId = null;
        _timers.CancelHeartbeat();
        _notifier.RoleChanged(_runtime.Role, _runtime.State.CurrentTerm);
        _notifier.LeaderChanged(null, _runtime.State.CurrentTerm);
        ScheduleElectionTimeout();
        _runtime.Logger.Info($"Node {_runtime.Cluster.LocalNodeId} resigned leadership.");
    }

    private void StepDownToFollower(RaftTerm term, NodeId? leaderId)
    {
        _timers.CancelHeartbeat();
        _election.BecomeFollower(term, leaderId, ScheduleElectionTimeout);
    }

    private void ScheduleElectionTimeout() =>
        _timers.ScheduleElectionTimeout(() => _ = _events.Writer.WriteAsync(new ElectionTimeoutEvent()));

    private void StartHeartbeatTimer() =>
        _timers.ScheduleHeartbeat(() => _ = _events.Writer.WriteAsync(new HeartbeatTickEvent()));
}
