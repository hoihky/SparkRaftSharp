using SparkRaftSharp.Models;
using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Persistence;

public sealed class FileLogStore : ILogStore
{
    private readonly string _path;
    private readonly List<RaftEntry> _entries = new();

    public FileLogStore(string directoryPath)
    {
        Directory.CreateDirectory(directoryPath);
        _path = Path.Combine(directoryPath, "raft-log.bin");
        LoadFromDisk();
    }

    public LogIndex LastIndex =>
        _entries.Count == 0 ? LogIndex.Zero : _entries[^1].Index;

    public RaftTerm GetTerm(LogIndex index)
    {
        var entry = GetEntry(index);
        return entry?.Term ?? RaftTerm.Zero;
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
        AppendToDisk(entry);
    }

    public void DeleteSuffixFrom(LogIndex fromIndexInclusive)
    {
        if (!fromIndexInclusive.IsValid)
            return;

        var keep = (int)(fromIndexInclusive.Value - 1);
        if (keep < 0)
            keep = 0;
        if (keep >= _entries.Count)
            return;

        _entries.RemoveRange(keep, _entries.Count - keep);
        RewriteDisk();
    }

    private void LoadFromDisk()
    {
        if (!File.Exists(_path))
            return;

        using var stream = File.OpenRead(_path);
        using var reader = new BinaryReader(stream);
        while (stream.Position < stream.Length)
        {
            var index = new LogIndex(reader.ReadInt64());
            var term = new RaftTerm(reader.ReadInt64());
            var length = reader.ReadInt32();
            var data = reader.ReadBytes(length);
            _entries.Add(new RaftEntry(index, term, data));
        }
    }

    private void AppendToDisk(RaftEntry entry)
    {
        using var stream = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.Read);
        using var writer = new BinaryWriter(stream);
        writer.Write(entry.Index.Value);
        writer.Write(entry.Term.Value);
        writer.Write(entry.Data.Length);
        writer.Write(entry.Data.Span);
    }

    private void RewriteDisk()
    {
        if (File.Exists(_path))
            File.Delete(_path);

        foreach (var entry in _entries)
            AppendToDisk(entry);
    }
}
