namespace SimpleCluster.Hosting;

public sealed class ClusterDemoApplication
{
    private readonly DemoConfiguration _configuration;
    private readonly List<ClusterNodeInstance> _nodes = new();
    private readonly List<ClientCommandServer> _clientServers = new();

    public ClusterDemoApplication(DemoConfiguration configuration) => _configuration = configuration;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        foreach (var member in _configuration.Members)
            _nodes.Add(new ClusterNodeInstance(member, _configuration.Members));

        foreach (var node in _nodes)
        {
            await node.StartAsync(cancellationToken);
            await Task.Delay(150, cancellationToken);
        }

        var gateway = new LeaderProposalGateway(_nodes);
        foreach (var node in _nodes)
        {
            var server = new ClientCommandServer(node, gateway);
            server.Start();
            _clientServers.Add(server);
        }

        Console.WriteLine("SparkRaftSharp SimpleCluster is running.");
        Console.WriteLine("Connect with: nc 127.0.0.1 7001");
        Console.WriteLine("Commands: SET key value | GET key | STATUS");
        Console.WriteLine("Press Ctrl+C to stop.");

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            foreach (var server in _clientServers)
                await server.DisposeAsync();
            foreach (var node in _nodes)
                await node.DisposeAsync();
        }
    }
}
