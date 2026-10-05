using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Messaging;

public sealed record RequestVoteResponse(
    NodeId SenderId,
    RaftTerm Term,
    bool VoteGranted) : IRaftMessage
{
    public RaftMessageKind Kind => RaftMessageKind.RequestVoteResult;
}
