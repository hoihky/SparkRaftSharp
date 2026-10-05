using System.Collections.Concurrent;
using SparkRaftSharp.Messaging;
using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Transport;

/// <summary>Loopback transport for tests and single-process clusters.</summary>
public sealed class InProcessRaftTransport : IRaftTransport
{
    private readonly ConcurrentDictionary<NodeId, Func<IRaftMessage, CancellationToken, Task>> _handlers = new();

    public void Register(NodeId localId, Func<IRaftMessage, CancellationToken, Task> handler)
    {
        _handlers[localId] = handler;
    }

    public void Unregister(NodeId localId) => _handlers.TryRemove(localId, out _);

    public Task SendAsync(NodeId targetId, IRaftMessage message, CancellationToken cancellationToken = default)
    {
        if (!_handlers.TryGetValue(targetId, out var handler))
            return Task.CompletedTask;

        return handler(message, cancellationToken);
    }
}
