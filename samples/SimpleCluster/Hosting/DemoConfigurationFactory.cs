using SparkRaftSharp.Primitives;

namespace SimpleCluster.Hosting;

public sealed class DemoConfigurationFactory
{
    public DemoConfiguration CreateThreeNodeCluster()
    {
        var members = new[]
        {
            new DemoMemberDefinition(new NodeId("n1"), 9001, 7001, "data/n1"),
            new DemoMemberDefinition(new NodeId("n2"), 9002, 7002, "data/n2"),
            new DemoMemberDefinition(new NodeId("n3"), 9003, 7003, "data/n3")
        };

        return new DemoConfiguration(members);
    }
}
