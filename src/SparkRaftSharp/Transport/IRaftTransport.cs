using SparkRaftSharp.Messaging;
using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Transport;

public interface IRaftTransport
{
    void Register(NodeId localId, Func<IRaftMessage, CancellationToken, Task> handler);

    Task SendAsync(NodeId targetId, IRaftMessage message, CancellationToken cancellationToken = default);
}
