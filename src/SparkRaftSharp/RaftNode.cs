using SparkRaftSharp.Internal;
using SparkRaftSharp.Models;
using SparkRaftSharp.Primitives;

namespace SparkRaftSharp;

internal sealed class RaftNode : IRaftNode
{
    private readonly RaftEngine _engine;

    public RaftNode(RaftEngine engine) => _engine = engine;

    public NodeId Id => _engine.NodeId;

    public Task StartAsync(CancellationToken cancellationToken = default) =>
        _engine.StartAsync(cancellationToken);

    public Task<LogIndex> ProposeAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) =>
        _engine.ProposeAsync(data, cancellationToken);

    public RaftNodeStatus GetStatus() => _engine.GetStatus();

    public Task ResignLeadershipAsync(CancellationToken cancellationToken = default) =>
        _engine.ResignLeadershipAsync(cancellationToken);

    public ValueTask DisposeAsync() => _engine.DisposeAsync();
}
