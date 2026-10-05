using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Messaging;

public sealed record AppendEntriesResponse(
    NodeId SenderId,
    RaftTerm Term,
    bool Success,
    LogIndex MatchIndex) : IRaftMessage
{
    public RaftMessageKind Kind => RaftMessageKind.AppendEntriesResult;
}
