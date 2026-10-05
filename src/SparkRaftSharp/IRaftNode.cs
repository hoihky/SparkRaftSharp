using SparkRaftSharp.Models;
using SparkRaftSharp.Primitives;

namespace SparkRaftSharp;

public interface IRaftNode : IAsyncDisposable
{
    NodeId Id { get; }

    Task StartAsync(CancellationToken cancellationToken = default);

    Task<LogIndex> ProposeAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);

    RaftNodeStatus GetStatus();

    /// <summary>Leader voluntarily steps down and becomes a follower in the current term.</summary>
    Task ResignLeadershipAsync(CancellationToken cancellationToken = default);
}
