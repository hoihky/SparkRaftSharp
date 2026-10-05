using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Messaging;

public sealed record RequestVoteRequest(
    NodeId SenderId,
    RaftTerm Term,
    NodeId CandidateId,
    LogIndex LastLogIndex,
    RaftTerm LastLogTerm) : IRaftMessage
{
    public RaftMessageKind Kind => RaftMessageKind.RequestVote;
}
