namespace SparkRaftSharp.Tests.Support;

internal sealed class ClusterSynchronization
{
    public async Task<IRaftNode> WaitForLeaderAsync(IReadOnlyList<IRaftNode> nodes, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var leader = nodes.FirstOrDefault(n => n.GetStatus().Role == Models.RaftRole.Leader);
            if (leader is not null)
                return leader;
            await Task.Delay(25);
        }

        throw new TimeoutException("No leader elected.");
    }

    public async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return;
            await Task.Delay(25);
        }

        throw new TimeoutException("Condition not met.");
    }
}
