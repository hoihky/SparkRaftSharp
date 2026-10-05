using System.Net;
using System.Net.Sockets;
using SparkRaftSharp.Messaging;
using SparkRaftSharp.Primitives;
using SparkRaftSharp.Transport;

namespace SimpleCluster.Networking;

public sealed class TcpRaftTransport : IRaftTransport, IAsyncDisposable
{
    private readonly NodeId _localId;
    private readonly RaftWireCodec _codec;
    private readonly IReadOnlyDictionary<NodeId, string> _peerHosts;
    private readonly TcpRaftListener _listener;
    private readonly RaftFrameChannel _frames = new();
    private Func<IRaftMessage, CancellationToken, Task>? _handler;

    public TcpRaftTransport(NodeId localId, IPEndPoint listenEndpoint, IReadOnlyDictionary<NodeId, string> peerHosts)
    {
        _localId = localId;
        _codec = new RaftWireCodec();
        _peerHosts = peerHosts;
        _listener = new TcpRaftListener(listenEndpoint, _frames, OnFrameReceivedAsync);
    }

    public void Register(NodeId localId, Func<IRaftMessage, CancellationToken, Task> handler)
    {
        if (localId != _localId)
            throw new InvalidOperationException("Local id mismatch.");

        _handler = handler;
        _listener.Start();
    }

    public async Task SendAsync(NodeId targetId, IRaftMessage message, CancellationToken cancellationToken = default)
    {
        if (!_peerHosts.TryGetValue(targetId, out var host))
            return;

        var endpoint = ParseEndpoint(host);
        using var client = new TcpClient();
        await client.ConnectAsync(endpoint.Address, endpoint.Port, cancellationToken).ConfigureAwait(false);
        await using var stream = client.GetStream();
        var bytes = _codec.Encode(message);
        await _frames.WriteAsync(stream, bytes, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync() => await _listener.DisposeAsync();

    private async Task OnFrameReceivedAsync(byte[] frame, CancellationToken cancellationToken)
    {
        if (_handler is null)
            return;

        try
        {
            var message = _codec.Decode(frame);
            await _handler(message, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{_localId}] Raft wire decode/dispatch failed: {ex.Message}");
        }
    }

    private static IPEndPoint ParseEndpoint(string host)
    {
        var parts = host.Split(':', 2);
        if (parts.Length != 2 || !int.TryParse(parts[1], out var port))
            throw new FormatException($"Invalid host:port '{host}'.");

        return new IPEndPoint(IPAddress.Parse(parts[0]), port);
    }
}
