namespace SparkRaftSharp.Configuration;

public sealed class RaftOptions
{
    public TimeSpan ElectionTimeoutMin { get; init; } = TimeSpan.FromMilliseconds(150);

    public TimeSpan ElectionTimeoutMax { get; init; } = TimeSpan.FromMilliseconds(300);

    public TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromMilliseconds(50);

    public bool CompleteProposalsWhenApplied { get; init; } = false;

    public void Validate()
    {
        if (ElectionTimeoutMin <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(ElectionTimeoutMin));
        if (ElectionTimeoutMax < ElectionTimeoutMin)
            throw new ArgumentOutOfRangeException(nameof(ElectionTimeoutMax));
        if (HeartbeatInterval <= TimeSpan.Zero || HeartbeatInterval >= ElectionTimeoutMin)
            throw new ArgumentOutOfRangeException(nameof(HeartbeatInterval),
                "Heartbeat must be positive and less than minimum election timeout.");
    }
}
