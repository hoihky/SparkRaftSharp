using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Models;

public sealed record RaftPeer(NodeId Id, string Address);
