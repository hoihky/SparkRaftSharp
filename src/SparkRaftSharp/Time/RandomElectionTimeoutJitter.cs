namespace SparkRaftSharp.Time;

public sealed class RandomElectionTimeoutJitter : IElectionTimeoutJitter
{
    private readonly Random _random = new();

    public TimeSpan NextDelay(TimeSpan minimum, TimeSpan maximum)
    {
        var rangeMs = (maximum - minimum).TotalMilliseconds;
        var extra = rangeMs > 0 ? _random.NextDouble() * rangeMs : 0;
        return minimum + TimeSpan.FromMilliseconds(extra);
    }
}
