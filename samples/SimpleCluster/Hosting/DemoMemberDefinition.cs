using SparkRaftSharp.Primitives;

namespace SimpleCluster.Hosting;

public sealed class DemoMemberDefinition
{
    public DemoMemberDefinition(NodeId id, int raftPort, int clientPort, string dataDirectory)
    {
        Id = id;
        RaftPort = raftPort;
        ClientPort = clientPort;
        DataDirectory = dataDirectory;
    }

    public NodeId Id { get; }

    public int RaftPort { get; }

    public int ClientPort { get; }

    public string DataDirectory { get; }

    public string RaftAddress => $"127.0.0.1:{RaftPort}";
}
