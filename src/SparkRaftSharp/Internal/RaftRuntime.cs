using SparkRaftSharp.Configuration;
using SparkRaftSharp.Logging;
using SparkRaftSharp.Models;
using SparkRaftSharp.Persistence;
using SparkRaftSharp.Primitives;
using SparkRaftSharp.StateMachine;
using SparkRaftSharp.Transport;

namespace SparkRaftSharp.Internal;

internal sealed class RaftRuntime
{
    public RaftRuntime(
        ClusterConfiguration cluster,
        RaftOptions options,
        ILogStore log,
        IStateStore stateStore,
        ISnapshotStore? snapshotStore,
        IRaftTransport transport,
        IRaftStateMachine stateMachine,
        IRaftLogger logger,
        RaftObserverNotifier notifier)
    {
        Cluster = cluster;
        Options = options;
        Log = log;
        StateStore = stateStore;
        SnapshotStore = snapshotStore;
        Transport = transport;
        StateMachine = stateMachine;
        Logger = logger;
        Notifier = notifier;
    }

    public ClusterConfiguration Cluster { get; }

    public RaftOptions Options { get; }

    public ILogStore Log { get; }

    public IStateStore StateStore { get; }

    public ISnapshotStore? SnapshotStore { get; }

    public IRaftTransport Transport { get; }

    public IRaftStateMachine StateMachine { get; }

    public IRaftLogger Logger { get; }

    public RaftObserverNotifier Notifier { get; }

    public PersistentState State { get; set; } = new();

    public RaftRole Role { get; set; } = RaftRole.Follower;

    public NodeId? LeaderId { get; set; }

    public int VotesReceived { get; set; }

    public Dictionary<LogIndex, TaskCompletionSource<LogIndex>> PendingProposals { get; } = new();

    public Dictionary<NodeId, LogIndex> NextIndex { get; } = new();

    public Dictionary<NodeId, LogIndex> MatchIndex { get; } = new();

    public void PersistState() => StateStore.Save(State);

    public RaftTerm GetLastLogTerm() =>
        Log.LastIndex.IsValid ? Log.GetTerm(Log.LastIndex) : RaftTerm.Zero;

    public bool IsLogUpToDate(LogIndex lastLogIndex, RaftTerm lastLogTerm)
    {
        var ourLastTerm = GetLastLogTerm();
        if (lastLogTerm.Value != ourLastTerm.Value)
            return lastLogTerm.Value > ourLastTerm.Value;
        return lastLogIndex.Value >= Log.LastIndex.Value;
    }

    public bool LogMatches(LogIndex prevIndex, RaftTerm prevTerm)
    {
        if (prevIndex.Value == 0)
            return true;
        return Log.GetTerm(prevIndex) == prevTerm;
    }
}
