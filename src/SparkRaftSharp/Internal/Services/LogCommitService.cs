using SparkRaftSharp.Primitives;
using SparkRaftSharp.StateMachine;

namespace SparkRaftSharp.Internal.Services;

internal sealed class LogCommitService
{
    private readonly RaftRuntime _runtime;
    private readonly RaftObserverNotifier _notifier;

    public LogCommitService(RaftRuntime runtime, RaftObserverNotifier notifier)
    {
        _runtime = runtime;
        _notifier = notifier;
    }

    public void TryAdvanceCommitIndex()
    {
        var target = _runtime.Log.LastIndex;
        while (target.IsValid && target.Value > _runtime.State.CommitIndex.Value)
        {
            if (_runtime.Log.GetTerm(target) != _runtime.State.CurrentTerm)
            {
                target = new LogIndex(target.Value - 1);
                continue;
            }

            var replicated = 1;
            foreach (var peer in _runtime.Cluster.RemotePeers)
            {
                if (_runtime.MatchIndex.GetValueOrDefault(peer.Id, LogIndex.Zero).Value >= target.Value)
                    replicated++;
            }

            if (replicated >= _runtime.Cluster.QuorumSize)
            {
                _runtime.State.CommitIndex = target;
                _runtime.PersistState();
                CompleteProposalsUpTo(_runtime.State.CommitIndex);
                _ = ApplyCommittedAsync();
                break;
            }

            target = new LogIndex(target.Value - 1);
        }
    }

    public async Task ApplyCommittedAsync()
    {
        while (_runtime.State.LastApplied.Value < _runtime.State.CommitIndex.Value)
        {
            var next = _runtime.State.LastApplied.Next;
            var entry = _runtime.Log.GetEntry(next);
            if (entry is null)
                break;

            await _runtime.StateMachine.ApplyAsync(entry).ConfigureAwait(false);
            _runtime.State.LastApplied = next;
            _runtime.PersistState();
            _notifier.EntryApplied(next);
        }
    }

    private void CompleteProposalsUpTo(LogIndex commitIndex)
    {
        foreach (var kv in _runtime.PendingProposals.ToArray())
        {
            if (kv.Key.Value <= commitIndex.Value)
            {
                _runtime.PendingProposals.Remove(kv.Key);
                kv.Value.TrySetResult(kv.Key);
                var term = _runtime.Log.GetTerm(kv.Key);
                _notifier.EntryCommitted(kv.Key, term);
            }
        }
    }
}
