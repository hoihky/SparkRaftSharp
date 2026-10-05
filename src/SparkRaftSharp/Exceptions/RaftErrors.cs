using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Exceptions;

public class RaftException : Exception
{
    public RaftException(string message) : base(message) { }

    public RaftException(string message, Exception inner) : base(message, inner) { }
}

public sealed class NotLeaderException : RaftException
{
    public NotLeaderException(NodeId? knownLeader)
        : base(knownLeader is null
            ? "This node is not the cluster leader."
            : $"This node is not the leader. Known leader: {knownLeader}.")
    {
        KnownLeader = knownLeader;
    }

    public NodeId? KnownLeader { get; }
}
