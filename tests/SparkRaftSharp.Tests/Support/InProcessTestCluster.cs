using SparkRaftSharp.Configuration;
using SparkRaftSharp.Models;
using SparkRaftSharp.Primitives;
using SparkRaftSharp.StateMachine;
using SparkRaftSharp.Transport;

namespace SparkRaftSharp.Tests.Support;

internal sealed class InProcessTestCluster
{
    private readonly InProcessRaftTransport _transport;
    private readonly IRaftNode[] _nodes;
    private readonly TestStateMachine[] _machines;

    private InProcessTestCluster(InProcessRaftTransport transport, IRaftNode[] nodes, TestStateMachine[] machines)
    {
        _transport = transport;
        _nodes = nodes;
        _machines = machines;
    }

    public IReadOnlyList<IRaftNode> Nodes => _nodes;

    public IReadOnlyList<TestStateMachine> StateMachines => _machines;

    public InProcessRaftTransport Transport => _transport;

    public async Task StartAsync()
    {
        foreach (var node in _nodes)
            await node.StartAsync();
    }

    public async Task DisposeAsync()
    {
        foreach (var node in _nodes)
            await node.DisposeAsync();
    }

    public IRaftNode? FindLeader() =>
        _nodes.FirstOrDefault(n => n.GetStatus().Role == RaftRole.Leader);

    public sealed class Factory
    {
        public async Task<InProcessTestCluster> CreateAsync(
            int nodeCount,
            TimeSpan? electionMin = null,
            TimeSpan? electionMax = null)
        {
            var transport = new InProcessRaftTransport();
            var ids = Enumerable.Range(1, nodeCount).Select(i => new NodeId($"n{i}")).ToArray();
            var peers = ids.Select(id => new RaftPeer(id, id.Value)).ToArray();
            var machines = ids.Select(_ => new TestStateMachine()).ToArray();

            var options = new RaftOptions
            {
                ElectionTimeoutMin = electionMin ?? TimeSpan.FromMilliseconds(80),
                ElectionTimeoutMax = electionMax ?? TimeSpan.FromMilliseconds(120),
                HeartbeatInterval = TimeSpan.FromMilliseconds(20)
            };

            var nodes = new IRaftNode[nodeCount];
            for (var i = 0; i < nodeCount; i++)
            {
                var nodeCluster = new ClusterConfiguration(ids[i], peers);
                nodes[i] = new RaftNodeBuilder()
                    .WithCluster(nodeCluster)
                    .WithTransport(transport)
                    .WithStateMachine(machines[i])
                    .WithOptions(options)
                    .Build();
            }

            var cluster = new InProcessTestCluster(transport, nodes, machines);
            await cluster.StartAsync();
            return cluster;
        }
    }
}
