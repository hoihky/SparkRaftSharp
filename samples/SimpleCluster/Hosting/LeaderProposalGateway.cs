using System.Text;
using SparkRaftSharp;
using SparkRaftSharp.Exceptions;
using SparkRaftSharp.Models;
using SparkRaftSharp.Primitives;

namespace SimpleCluster.Hosting;

public sealed class LeaderProposalGateway
{
    private readonly IReadOnlyList<ClusterNodeInstance> _nodes;

    public LeaderProposalGateway(IReadOnlyList<ClusterNodeInstance> nodes) => _nodes = nodes;

    public async Task<LogIndex> ProposeOnLeaderAsync(string command, CancellationToken cancellationToken)
    {
        var payload = Encoding.UTF8.GetBytes(command);
        var leader = _nodes.FirstOrDefault(n => n.RaftNode.GetStatus().Role == RaftRole.Leader);
        if (leader is not null)
        {
            try
            {
                return await leader.RaftNode.ProposeAsync(payload, cancellationToken);
            }
            catch (NotLeaderException)
            {
                // Fall through and retry discovery.
            }
        }

        foreach (var node in _nodes)
        {
            try
            {
                return await node.RaftNode.ProposeAsync(payload, cancellationToken);
            }
            catch (NotLeaderException ex) when (ex.KnownLeader is not null)
            {
                var redirect = _nodes.FirstOrDefault(n => n.Id == ex.KnownLeader);
                if (redirect is not null)
                    return await redirect.RaftNode.ProposeAsync(payload, cancellationToken);
            }
            catch (NotLeaderException)
            {
            }
        }

        throw new InvalidOperationException("No cluster leader is available.");
    }
}
