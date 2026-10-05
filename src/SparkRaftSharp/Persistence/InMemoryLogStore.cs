using SparkRaftSharp.Models;
using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Persistence;

public sealed class InMemoryLogStore : ILogStore
{
    private readonly List<RaftEntry> _entries = new();

    public LogIndex LastIndex =>
        _entries.Count == 0 ? LogIndex.Zero : _entries[^1].Index;

    public RaftTerm GetTerm(LogIndex index)
    {
        if (!index.IsValid || index.Value > _entries.Count)
            return RaftTerm.Zero;
        return _entries[(int)(index.Value - 1)].Term;
    }

    public RaftEntry? GetEntry(LogIndex index)
    {
        if (!index.IsValid || index.Value > _entries.Count)
            return null;
        return _entries[(int)(index.Value - 1)];
    }

    public void Append(RaftEntry entry)
    {
        if (entry.Index.Value != _entries.Count + 1)
            throw new InvalidOperationException("Log append must be sequential.");

        _entries.Add(entry);
    }

    public void DeleteSuffixFrom(LogIndex fromIndexInclusive)
    {
        if (!fromIndexInclusive.IsValid)
            return;

        var keep = (int)(fromIndexInclusive.Value - 1);
        if (keep < 0)
            keep = 0;
        if (keep > _entries.Count)
            return;

        _entries.RemoveRange(keep, _entries.Count - keep);
    }
}
