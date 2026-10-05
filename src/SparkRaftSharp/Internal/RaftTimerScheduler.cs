using SparkRaftSharp.Configuration;
using SparkRaftSharp.Time;

namespace SparkRaftSharp.Internal;

internal sealed class RaftTimerScheduler : IDisposable
{
    private readonly IRaftClock _clock;
    private readonly RaftOptions _options;
    private readonly IElectionTimeoutJitter _jitter;
    private readonly object _gate = new();
    private IDisposable? _electionTimer;
    private IDisposable? _heartbeatTimer;

    public RaftTimerScheduler(IRaftClock clock, RaftOptions options, IElectionTimeoutJitter jitter)
    {
        _clock = clock;
        _options = options;
        _jitter = jitter;
    }

    public void ScheduleElectionTimeout(Action onTimeout)
    {
        CancelElection();
        var delay = _jitter.NextDelay(_options.ElectionTimeoutMin, _options.ElectionTimeoutMax);
        lock (_gate)
        {
            _electionTimer = _clock.Schedule(delay, onTimeout);
        }
    }

    public void ScheduleHeartbeat(Action onHeartbeat)
    {
        CancelHeartbeat();
        lock (_gate)
        {
            _heartbeatTimer = new RecurringHeartbeat(_clock, _options.HeartbeatInterval, onHeartbeat);
        }
    }

    public void CancelElection()
    {
        lock (_gate)
        {
            _electionTimer?.Dispose();
            _electionTimer = null;
        }
    }

    public void CancelHeartbeat()
    {
        lock (_gate)
        {
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = null;
        }
    }

    public void Dispose()
    {
        CancelElection();
        CancelHeartbeat();
    }

    private sealed class RecurringHeartbeat : IDisposable
    {
        private readonly Timer _timer;

        public RecurringHeartbeat(IRaftClock clock, TimeSpan interval, Action callback)
        {
            _timer = new Timer(_ => callback(), null, interval, interval);
        }

        public void Dispose() => _timer.Dispose();
    }
}
