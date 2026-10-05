using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Persistence;

public sealed class PersistentState
{
    public RaftTerm CurrentTerm { get; set; } = RaftTerm.Zero;

    public NodeId? VotedFor { get; set; }

    public LogIndex CommitIndex { get; set; } = LogIndex.Zero;

    public LogIndex LastApplied { get; set; } = LogIndex.Zero;
}
