namespace SparkRaftSharp.Time;

public interface IRaftClock
{
    DateTimeOffset UtcNow { get; }

    /// <summary>Schedules a one-shot callback. Dispose to cancel.</summary>
    IDisposable Schedule(TimeSpan delay, Action callback);
}
