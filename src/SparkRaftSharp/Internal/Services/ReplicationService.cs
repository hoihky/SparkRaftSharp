using SparkRaftSharp.Messaging;
using SparkRaftSharp.Models;
using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Internal.Services;

internal sealed class ReplicationService
{
    private readonly RaftRuntime _runtime;
    private readonly LogCommitService _commitService;

    public ReplicationService(RaftRuntime runtime, LogCommitService commitService)
    {
        _runtime = runtime;
        _commitService = commitService;
    }

    public async Task ReplicateToAllPeersAsync()
    {
        var tasks = _runtime.Cluster.RemotePeers.Select(p => ReplicateToPeerAsync(p.Id)).ToArray();
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async Task ReplicateToPeerAsync(NodeId peerId)
    {
        if (_runtime.Role != RaftRole.Leader)
            return;

        var snapshot = _runtime.SnapshotStore?.GetLatest();
        var nextIndex = _runtime.NextIndex.GetValueOrDefault(peerId, _runtime.Log.LastIndex.Next);

        if (snapshot is not null &&
            nextIndex.Value <= snapshot.LastIncludedIndex.Next.Value)
        {
            await SendInstallSnapshotAsync(peerId, snapshot).ConfigureAwait(false);
            return;
        }

        var prevIndex = nextIndex.Value > 1 ? new LogIndex(nextIndex.Value - 1) : LogIndex.Zero;
        var prevTerm = prevIndex.IsValid ? _runtime.Log.GetTerm(prevIndex) : RaftTerm.Zero;

        var entries = new List<RaftEntry>();
        for (var i = nextIndex.Value; i <= _runtime.Log.LastIndex.Value; i++)
        {
            var entry = _runtime.Log.GetEntry(new LogIndex(i));
            if (entry is not null)
                entries.Add(entry);
        }

        var request = new AppendEntriesRequest(
            _runtime.Cluster.LocalNodeId,
            _runtime.State.CurrentTerm,
            _runtime.Cluster.LocalNodeId,
            prevIndex,
            prevTerm,
            entries,
            _runtime.State.CommitIndex);

        await _runtime.Transport.SendAsync(peerId, request).ConfigureAwait(false);
    }

    public void HandleAppendEntriesResponse(AppendEntriesResponse response, Action becomeFollower)
    {
        if (response.Term.Value > _runtime.State.CurrentTerm.Value)
        {
            becomeFollower();
            return;
        }

        if (_runtime.Role != RaftRole.Leader || response.Term != _runtime.State.CurrentTerm)
            return;

        var peerId = response.SenderId;
        if (response.Success)
        {
            _runtime.MatchIndex[peerId] = response.MatchIndex;
            _runtime.NextIndex[peerId] = response.MatchIndex.Next;
            _commitService.TryAdvanceCommitIndex();
        }
        else
        {
            var next = _runtime.NextIndex.GetValueOrDefault(peerId, _runtime.Log.LastIndex.Next);
            if (next.Value > 1)
                _runtime.NextIndex[peerId] = new LogIndex(next.Value - 1);
            _ = ReplicateToPeerAsync(peerId);
        }
    }

    public void HandleInstallSnapshotResponse(InstallSnapshotResponse response, Action becomeFollower)
    {
        if (response.Term.Value > _runtime.State.CurrentTerm.Value)
        {
            becomeFollower();
            return;
        }

        if (_runtime.Role != RaftRole.Leader || response.Term != _runtime.State.CurrentTerm)
            return;

        if (!response.Success)
            return;

        var snapshot = _runtime.SnapshotStore?.GetLatest();
        if (snapshot is null)
            return;

        _runtime.MatchIndex[response.SenderId] = snapshot.LastIncludedIndex;
        _runtime.NextIndex[response.SenderId] = snapshot.LastIncludedIndex.Next;
        _commitService.TryAdvanceCommitIndex();
    }

    public void InitializeLeaderIndices()
    {
        _runtime.NextIndex.Clear();
        _runtime.MatchIndex.Clear();
        foreach (var peer in _runtime.Cluster.RemotePeers)
        {
            _runtime.NextIndex[peer.Id] = _runtime.Log.LastIndex.Next;
            _runtime.MatchIndex[peer.Id] = LogIndex.Zero;
        }
    }

    private async Task SendInstallSnapshotAsync(NodeId peerId, Persistence.RaftSnapshot snapshot)
    {
        var request = new InstallSnapshotRequest(
            _runtime.Cluster.LocalNodeId,
            _runtime.State.CurrentTerm,
            _runtime.Cluster.LocalNodeId,
            snapshot.LastIncludedIndex,
            snapshot.LastIncludedTerm,
            snapshot.Data);

        await _runtime.Transport.SendAsync(peerId, request).ConfigureAwait(false);
    }
}
