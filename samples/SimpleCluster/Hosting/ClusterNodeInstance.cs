using System.Net;
using SparkRaftSharp;
using SparkRaftSharp.Configuration;
using SparkRaftSharp.Logging;
using SparkRaftSharp.Models;
using SparkRaftSharp.Persistence;
using SparkRaftSharp.Primitives;
using SimpleCluster.Networking;
using SimpleCluster.State;

namespace SimpleCluster.Hosting;

public sealed class ClusterNodeInstance : IAsyncDisposable
{
    private readonly DemoMemberDefinition _member;
    private readonly IReadOnlyList<DemoMemberDefinition> _allMembers;
    private readonly ConsoleRaftLogger _logger;
    private readonly KeyValueStateMachine _stateMachine = new();
    private readonly TcpRaftTransport _transport;
    private readonly IRaftNode _raftNode;

    public ClusterNodeInstance(DemoMemberDefinition member, IReadOnlyList<DemoMemberDefinition> allMembers)
    {
        _member = member;
        _allMembers = allMembers;
        _logger = new ConsoleRaftLogger(member.Id);

        var peers = allMembers
            .Select(m => new RaftPeer(m.Id, m.RaftAddress))
            .ToArray();

        var peerMap = allMembers.ToDictionary(m => m.Id, m => m.RaftAddress);
        var listen = new IPEndPoint(IPAddress.Loopback, member.RaftPort);
        _transport = new TcpRaftTransport(member.Id, listen, peerMap);

        Directory.CreateDirectory(member.DataDirectory);
        var options = new RaftOptions
        {
            ElectionTimeoutMin = TimeSpan.FromMilliseconds(400),
            ElectionTimeoutMax = TimeSpan.FromMilliseconds(800),
            HeartbeatInterval = TimeSpan.FromMilliseconds(120)
        };

        _raftNode = new RaftNodeBuilder()
            .WithCluster(new ClusterConfiguration(member.Id, peers))
            .WithTransport(_transport)
            .WithStateMachine(_stateMachine)
            .WithLogStore(new FileLogStore(member.DataDirectory))
            .WithStateStore(new FileStateStore(member.DataDirectory))
            .WithSnapshotStore(new InMemorySnapshotStore())
            .WithOptions(options)
            .WithLogger(_logger)
            .Build();
    }

    public NodeId Id => _member.Id;

    public IRaftNode RaftNode => _raftNode;

    public KeyValueStateMachine StateMachine => _stateMachine;

    public int ClientPort => _member.ClientPort;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _raftNode.StartAsync(cancellationToken);
        _logger.Info($"Raft node listening on {_member.RaftAddress}, client API on port {_member.ClientPort}");
    }

    public async ValueTask DisposeAsync()
    {
        await _raftNode.DisposeAsync();
        await _transport.DisposeAsync();
    }
}
