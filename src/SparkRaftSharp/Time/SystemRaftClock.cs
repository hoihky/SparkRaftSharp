namespace SparkRaftSharp.Time;

public sealed class SystemRaftClock : IRaftClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public IDisposable Schedule(TimeSpan delay, Action callback)
    {
        var timer = new Timer(static state =>
        {
            var cb = (Action)state!;
            cb();
        }, callback, delay, Timeout.InfiniteTimeSpan);

        return new TimerDisposable(timer);
    }

    private sealed class TimerDisposable(Timer timer) : IDisposable
    {
        public void Dispose() => timer.Dispose();
    }
}
