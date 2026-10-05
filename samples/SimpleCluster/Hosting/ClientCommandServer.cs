using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SimpleCluster.Hosting;

public sealed class ClientCommandServer : IAsyncDisposable
{
    private readonly ClusterNodeInstance _node;
    private readonly LeaderProposalGateway _gateway;
    private readonly TcpListener _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptLoop;

    public ClientCommandServer(ClusterNodeInstance node, LeaderProposalGateway gateway)
    {
        _node = node;
        _gateway = gateway;
        _listener = new TcpListener(IPAddress.Loopback, node.ClientPort);
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _listener.Start();
        _acceptLoop = Task.Run(() => AcceptLoopAsync(_cts.Token));
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        _listener.Stop();
        if (_acceptLoop is not null)
        {
            try { await _acceptLoop.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            await using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };

            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
                return;

            var response = await ExecuteAsync(line, cancellationToken).ConfigureAwait(false);
            await writer.WriteLineAsync(response).ConfigureAwait(false);
        }
    }

    private async Task<string> ExecuteAsync(string line, CancellationToken cancellationToken)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return "ERR empty";

        if (parts[0].Equals("GET", StringComparison.OrdinalIgnoreCase) && parts.Length == 2)
        {
            var value = _node.StateMachine.Get(parts[1]);
            return value is null ? "OK null" : $"OK {value}";
        }

        if (parts[0].Equals("SET", StringComparison.OrdinalIgnoreCase) && parts.Length == 3)
        {
            var command = $"SET {parts[1]} {parts[2]}";
            var index = await _gateway.ProposeOnLeaderAsync(command, cancellationToken).ConfigureAwait(false);
            return $"OK committed={index.Value}";
        }

        if (parts[0].Equals("STATUS", StringComparison.OrdinalIgnoreCase))
        {
            var status = _node.RaftNode.GetStatus();
            return $"OK role={status.Role} term={status.CurrentTerm.Value} leader={status.LeaderId}";
        }

        return "ERR unknown command";
    }
}
