using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Messaging;

public sealed record InstallSnapshotResponse(
    NodeId SenderId,
    RaftTerm Term,
    bool Success) : IRaftMessage
{
    public RaftMessageKind Kind => RaftMessageKind.InstallSnapshotResult;
}
