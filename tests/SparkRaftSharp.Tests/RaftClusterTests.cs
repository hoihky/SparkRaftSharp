using System.Text;
using SparkRaftSharp.Primitives;
using SparkRaftSharp.Tests.Support;

namespace SparkRaftSharp.Tests;

public class RaftClusterTests
{
    private readonly InProcessTestCluster.Factory _clusterFactory = new();
    private readonly ClusterSynchronization _sync = new();

    [Fact]
    public async Task Three_nodes_elect_a_leader()
    {
        var cluster = await _clusterFactory.CreateAsync(3);
        await _sync.WaitForLeaderAsync(cluster.Nodes, TimeSpan.FromSeconds(3));

        var leaders = cluster.Nodes.Count(n => n.GetStatus().Role == Models.RaftRole.Leader);
        Assert.Equal(1, leaders);
        await cluster.DisposeAsync();
    }

    [Fact]
    public async Task Leader_replicates_command_to_majority()
    {
        var cluster = await _clusterFactory.CreateAsync(3);
        var leader = await _sync.WaitForLeaderAsync(cluster.Nodes, TimeSpan.FromSeconds(3));

        var payload = Encoding.UTF8.GetBytes("set-key=1");
        var index = await leader.ProposeAsync(payload);

        await _sync.WaitUntilAsync(
            () => cluster.Nodes.All(n => n.GetStatus().CommitIndex.Value >= index.Value),
            TimeSpan.FromSeconds(3));

        await cluster.DisposeAsync();
    }

    [Fact]
    public async Task New_leader_continues_after_previous_leader_stops()
    {
        var cluster = await _clusterFactory.CreateAsync(3);
        var leader = await _sync.WaitForLeaderAsync(cluster.Nodes, TimeSpan.FromSeconds(3));

        var first = await leader.ProposeAsync(Encoding.UTF8.GetBytes("seed"));
        await _sync.WaitUntilAsync(
            () => cluster.Nodes.All(n => n.GetStatus().CommitIndex.Value >= first.Value),
            TimeSpan.FromSeconds(3));

        await leader.DisposeAsync();
        var survivors = cluster.Nodes.Where(n => n != leader).ToList();

        var newLeader = await _sync.WaitForLeaderAsync(survivors, TimeSpan.FromSeconds(5));
        var second = await newLeader.ProposeAsync(Encoding.UTF8.GetBytes("after-failover"));
        await _sync.WaitUntilAsync(
            () => survivors.All(n => n.GetStatus().CommitIndex.Value >= second.Value),
            TimeSpan.FromSeconds(5));

        await cluster.DisposeAsync();
    }
}
