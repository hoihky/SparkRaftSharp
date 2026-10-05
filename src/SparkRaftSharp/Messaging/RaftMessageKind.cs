namespace SparkRaftSharp.Messaging;

public enum RaftMessageKind
{
    RequestVote,
    RequestVoteResult,
    AppendEntries,
    AppendEntriesResult,
    InstallSnapshot,
    InstallSnapshotResult
}
