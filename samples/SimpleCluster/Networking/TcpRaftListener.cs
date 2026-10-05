using System.Net;
using System.Net.Sockets;

namespace SimpleCluster.Networking;

public sealed class TcpRaftListener : IAsyncDisposable
{
    private readonly IPEndPoint _endpoint;
    private readonly RaftFrameChannel _frames;
    private readonly Func<byte[], CancellationToken, Task> _onFrame;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptLoop;

    public TcpRaftListener(IPEndPoint endpoint, RaftFrameChannel frames, Func<byte[], CancellationToken, Task> onFrame)
    {
        _endpoint = endpoint;
        _frames = frames;
        _onFrame = onFrame;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _listener = new TcpListener(_endpoint);
        _listener.Start();
        _acceptLoop = Task.Run(() => AcceptLoopAsync(_cts.Token));
    }

    public async ValueTask DisposeAsync()
    {
        if (_cts is not null)
        {
            _cts.Cancel();
            _listener?.Stop();
            if (_acceptLoop is not null)
            {
                try { await _acceptLoop.ConfigureAwait(false); }
                catch (OperationCanceledException) { }
            }
        }
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var client = await _listener!.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            await using var stream = client.GetStream();
            var frame = await _frames.ReadAsync(stream, cancellationToken).ConfigureAwait(false);
            if (frame.Length > 0)
                await _onFrame(frame, cancellationToken).ConfigureAwait(false);
        }
    }
}
