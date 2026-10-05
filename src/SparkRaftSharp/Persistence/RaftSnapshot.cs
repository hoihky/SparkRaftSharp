using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Persistence;

public sealed record RaftSnapshot(
    LogIndex LastIncludedIndex,
    RaftTerm LastIncludedTerm,
    ReadOnlyMemory<byte> Data);
