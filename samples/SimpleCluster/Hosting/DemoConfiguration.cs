using SparkRaftSharp.Primitives;

namespace SimpleCluster.Hosting;

public sealed class DemoConfiguration
{
    public DemoConfiguration(IReadOnlyList<DemoMemberDefinition> members)
    {
        if (members.Count == 0)
            throw new ArgumentException("At least one member is required.");

        Members = members;
    }

    public IReadOnlyList<DemoMemberDefinition> Members { get; }
}
