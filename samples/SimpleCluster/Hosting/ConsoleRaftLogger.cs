using SparkRaftSharp.Logging;
using SparkRaftSharp.Primitives;

namespace SimpleCluster.Hosting;

public sealed class ConsoleRaftLogger : IRaftLogger
{
    private readonly NodeId _nodeId;

    public ConsoleRaftLogger(NodeId nodeId) => _nodeId = nodeId;

    public void Debug(string message) => Write("DBG", message);

    public void Info(string message) => Write("INF", message);

    public void Warn(string message) => Write("WRN", message);

    public void Error(string message, Exception? exception = null) =>
        Write("ERR", exception is null ? message : $"{message} ({exception.Message})");

    private void Write(string level, string message) =>
        Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}] [{_nodeId}] {level} {message}");
}
