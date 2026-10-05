using SparkRaftSharp.Models;
using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Observers;

public interface IRaftObserver
{
    void OnRoleChanged(RaftRole role, RaftTerm term);

    void OnLeaderChanged(NodeId? leaderId, RaftTerm term);

    void OnEntryCommitted(LogIndex index, RaftTerm term);

    void OnEntryApplied(LogIndex index);
}
