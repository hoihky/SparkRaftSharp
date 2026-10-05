using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Models;

public sealed class ClusterConfiguration
{
    public ClusterConfiguration(NodeId localNodeId, IReadOnlyList<RaftPeer> peers)
    {
        if (peers.Count == 0)
            throw new ArgumentException("At least one peer (self) is required.", nameof(peers));

        LocalNodeId = localNodeId;
        Peers = peers;
        if (!peers.Any(p => p.Id == localNodeId))
            throw new ArgumentException("Peers must include the local node.", nameof(peers));
    }

    public NodeId LocalNodeId { get; }

    public IReadOnlyList<RaftPeer> Peers { get; }

    public int QuorumSize => Peers.Count / 2 + 1;

    public IEnumerable<RaftPeer> RemotePeers =>
        Peers.Where(p => p.Id != LocalNodeId);
}
