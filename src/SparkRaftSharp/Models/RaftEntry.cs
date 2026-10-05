using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Models;

/// <summary>A single command in the replicated log.</summary>
public sealed record RaftEntry(LogIndex Index, RaftTerm Term, ReadOnlyMemory<byte> Data);
