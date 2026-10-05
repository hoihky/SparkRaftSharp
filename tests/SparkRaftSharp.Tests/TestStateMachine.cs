using System.Collections.Concurrent;
using SparkRaftSharp.Models;
using SparkRaftSharp.Persistence;
using SparkRaftSharp.StateMachine;

namespace SparkRaftSharp.Tests;

internal sealed class TestStateMachine : IRaftStateMachine
{
    private readonly ConcurrentQueue<RaftEntry> _applied = new();

    public IReadOnlyCollection<RaftEntry> Applied => _applied.ToArray();

    public ValueTask ApplyAsync(RaftEntry entry, CancellationToken cancellationToken = default)
    {
        _applied.Enqueue(entry);
        return ValueTask.CompletedTask;
    }

    public ValueTask ApplySnapshotAsync(RaftSnapshot snapshot, CancellationToken cancellationToken = default) =>
        ValueTask.CompletedTask;
}
