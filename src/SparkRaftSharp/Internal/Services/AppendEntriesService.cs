using SparkRaftSharp.Messaging;
using SparkRaftSharp.Models;
using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Internal.Services;

internal sealed class AppendEntriesService
{
    private readonly RaftRuntime _runtime;
    private readonly LogCommitService _commitService;

    public AppendEntriesService(RaftRuntime runtime, LogCommitService commitService)
    {
        _runtime = runtime;
        _commitService = commitService;
    }

    public async Task HandleAsync(AppendEntriesRequest request, Action scheduleElectionTimeout, Action becomeFollower)
    {
        var success = false;
        var matchIndex = LogIndex.Zero;

        if (request.Term.Value > _runtime.State.CurrentTerm.Value)
            becomeFollower();

        if (request.Term.Value >= _runtime.State.CurrentTerm.Value)
        {
            _runtime.Role = RaftRole.Follower;
            _runtime.LeaderId = request.LeaderId;
            scheduleElectionTimeout();

            if (_runtime.LogMatches(request.PrevLogIndex, request.PrevLogTerm))
            {
                var index = request.PrevLogIndex.Next.Value;
                foreach (var entry in request.Entries)
                {
                    var logIndex = new LogIndex(index);
                    var existing = _runtime.Log.GetEntry(logIndex);
                    if (existing is not null && existing.Term != entry.Term)
                    {
                        _runtime.Log.DeleteSuffixFrom(logIndex);
                        existing = null;
                    }

                    if (existing is null)
                    {
                        var toAppend = entry with { Index = logIndex };
                        _runtime.Log.Append(toAppend);
                    }

                    index++;
                }

                if (request.LeaderCommit.Value > _runtime.State.CommitIndex.Value)
                {
                    var lastNew = request.Entries.Count == 0
                        ? request.PrevLogIndex
                        : new LogIndex(request.PrevLogIndex.Value + request.Entries.Count);
                    _runtime.State.CommitIndex = LogIndex.Min(request.LeaderCommit, lastNew);
                    _runtime.PersistState();
                    await _commitService.ApplyCommittedAsync().ConfigureAwait(false);
                }

                success = true;
                matchIndex = _runtime.Log.LastIndex;
            }
        }

        var response = new AppendEntriesResponse(
            _runtime.Cluster.LocalNodeId,
            _runtime.State.CurrentTerm,
            success,
            matchIndex);

        await _runtime.Transport.SendAsync(request.SenderId, response).ConfigureAwait(false);
    }
}
