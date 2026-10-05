using System.Text.Json;
using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Persistence;

public sealed class FileStateStore : IStateStore
{
    private readonly string _path;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = false };

    public FileStateStore(string directoryPath)
    {
        Directory.CreateDirectory(directoryPath);
        _path = Path.Combine(directoryPath, "raft-state.json");
    }

    public PersistentState Load()
    {
        if (!File.Exists(_path))
            return new PersistentState();

        var json = File.ReadAllText(_path);
        var dto = JsonSerializer.Deserialize<StateDto>(json, _jsonOptions);
        if (dto is null)
            return new PersistentState();

        return new PersistentState
        {
            CurrentTerm = new RaftTerm(dto.CurrentTerm),
            VotedFor = string.IsNullOrEmpty(dto.VotedFor) ? null : new NodeId(dto.VotedFor),
            CommitIndex = new LogIndex(dto.CommitIndex),
            LastApplied = new LogIndex(dto.LastApplied)
        };
    }

    public void Save(PersistentState state)
    {
        var dto = new StateDto
        {
            CurrentTerm = state.CurrentTerm.Value,
            VotedFor = state.VotedFor?.Value ?? string.Empty,
            CommitIndex = state.CommitIndex.Value,
            LastApplied = state.LastApplied.Value
        };

        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        File.WriteAllText(_path, json);
    }

    private sealed class StateDto
    {
        public long CurrentTerm { get; set; }
        public string VotedFor { get; set; } = string.Empty;
        public long CommitIndex { get; set; }
        public long LastApplied { get; set; }
    }
}
