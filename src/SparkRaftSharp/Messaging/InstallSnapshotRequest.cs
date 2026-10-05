using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Messaging;

public sealed record InstallSnapshotRequest(
    NodeId SenderId,
    RaftTerm Term,
    NodeId LeaderId,
    LogIndex LastIncludedIndex,
    RaftTerm LastIncludedTerm,
    ReadOnlyMemory<byte> Data) : IRaftMessage
{
    public RaftMessageKind Kind => RaftMessageKind.InstallSnapshot;
}
