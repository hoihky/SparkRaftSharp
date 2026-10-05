using SparkRaftSharp.Models;
using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Messaging;

public sealed record AppendEntriesRequest(
    NodeId SenderId,
    RaftTerm Term,
    NodeId LeaderId,
    LogIndex PrevLogIndex,
    RaftTerm PrevLogTerm,
    IReadOnlyList<RaftEntry> Entries,
    LogIndex LeaderCommit) : IRaftMessage
{
    public RaftMessageKind Kind => RaftMessageKind.AppendEntries;
}
