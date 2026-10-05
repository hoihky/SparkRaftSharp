using SparkRaftSharp.Messaging;
using SparkRaftSharp.Models;
using SparkRaftSharp.Persistence;
using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Internal.Services;

internal sealed class InstallSnapshotService
{
    private readonly RaftRuntime _runtime;
    private readonly LogCommitService _commitService;

    public InstallSnapshotService(RaftRuntime runtime, LogCommitService commitService)
    {
        _runtime = runtime;
        _commitService = commitService;
    }

    public async Task HandleAsync(InstallSnapshotRequest request, Action scheduleElectionTimeout, Action becomeFollower)
    {
        var success = false;

        if (request.Term.Value > _runtime.State.CurrentTerm.Value)
            becomeFollower();

        if (request.Term.Value < _runtime.State.CurrentTerm.Value)
        {
            await ReplyAsync(request.SenderId, false).ConfigureAwait(false);
            return;
        }

        _runtime.Role = RaftRole.Follower;
        _runtime.LeaderId = request.LeaderId;
        scheduleElectionTimeout();

        var existing = _runtime.SnapshotStore?.GetLatest();
        if (existing is null || existing.LastIncludedIndex.Value < request.LastIncludedIndex.Value)
        {
            var snapshot = new RaftSnapshot(
                request.LastIncludedIndex,
                request.LastIncludedTerm,
                request.Data);

            _runtime.SnapshotStore?.Save(snapshot);
            _runtime.Log.DeleteSuffixFrom(request.LastIncludedIndex.Next);
            await _runtime.StateMachine.ApplySnapshotAsync(snapshot).ConfigureAwait(false);

            if (request.LastIncludedIndex.Value > _runtime.State.LastApplied.Value)
                _runtime.State.LastApplied = request.LastIncludedIndex;

            if (request.LastIncludedIndex.Value > _runtime.State.CommitIndex.Value)
                _runtime.State.CommitIndex = request.LastIncludedIndex;

            _runtime.PersistState();
        }

        success = true;
        await ReplyAsync(request.SenderId, success).ConfigureAwait(false);
    }

    private async Task ReplyAsync(NodeId senderId, bool success)
    {
        var response = new InstallSnapshotResponse(
            _runtime.Cluster.LocalNodeId,
            _runtime.State.CurrentTerm,
            success);

        await _runtime.Transport.SendAsync(senderId, response).ConfigureAwait(false);
    }
}
