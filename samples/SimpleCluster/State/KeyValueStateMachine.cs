using System.Collections.Concurrent;
using System.Text;
using SparkRaftSharp.Models;
using SparkRaftSharp.Persistence;
using SparkRaftSharp.Primitives;
using SparkRaftSharp.StateMachine;

namespace SimpleCluster.State;

public sealed class KeyValueStateMachine : IRaftStateMachine
{
    private readonly ConcurrentDictionary<string, string> _values = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, string> Values => _values;

    public ValueTask ApplyAsync(RaftEntry entry, CancellationToken cancellationToken = default)
    {
        var text = Encoding.UTF8.GetString(entry.Data.Span);
        var parts = text.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3 && parts[0].Equals("SET", StringComparison.OrdinalIgnoreCase))
            _values[parts[1]] = parts[2];

        return ValueTask.CompletedTask;
    }

    public ValueTask ApplySnapshotAsync(RaftSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        _values.Clear();
        var text = Encoding.UTF8.GetString(snapshot.Data.Span);
        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = line.IndexOf('=');
            if (idx > 0)
                _values[line[..idx]] = line[(idx + 1)..];
        }

        return ValueTask.CompletedTask;
    }

    public RaftSnapshot CaptureSnapshot(LogIndex lastIncludedIndex, RaftTerm lastIncludedTerm)
    {
        var builder = new StringBuilder();
        foreach (var pair in _values.OrderBy(p => p.Key))
            builder.AppendLine($"{pair.Key}={pair.Value}");

        return new RaftSnapshot(
            lastIncludedIndex,
            lastIncludedTerm,
            Encoding.UTF8.GetBytes(builder.ToString()));
    }

    public string? Get(string key) => _values.TryGetValue(key, out var value) ? value : null;
}
