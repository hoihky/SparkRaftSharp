using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Messaging;

public interface IRaftMessage
{
    RaftMessageKind Kind { get; }

    NodeId SenderId { get; }
}
