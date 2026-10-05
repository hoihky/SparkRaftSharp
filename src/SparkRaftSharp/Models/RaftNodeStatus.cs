using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Models;

public sealed record RaftNodeStatus(
    RaftRole Role,
    RaftTerm CurrentTerm,
    NodeId? LeaderId,
    LogIndex CommitIndex,
    LogIndex LastApplied,
    LogIndex LastLogIndex);
