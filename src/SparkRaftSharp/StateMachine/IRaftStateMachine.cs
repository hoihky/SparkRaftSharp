using SparkRaftSharp.Models;
using SparkRaftSharp.Persistence;

namespace SparkRaftSharp.StateMachine;

public interface IRaftStateMachine
{
    ValueTask ApplyAsync(RaftEntry entry, CancellationToken cancellationToken = default);

    ValueTask ApplySnapshotAsync(RaftSnapshot snapshot, CancellationToken cancellationToken = default);
}
