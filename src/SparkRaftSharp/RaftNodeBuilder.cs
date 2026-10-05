using SparkRaftSharp.Configuration;
using SparkRaftSharp.Internal;
using SparkRaftSharp.Logging;
using SparkRaftSharp.Models;
using SparkRaftSharp.Observers;
using SparkRaftSharp.Persistence;
using SparkRaftSharp.StateMachine;
using SparkRaftSharp.Time;
using SparkRaftSharp.Transport;

namespace SparkRaftSharp;

public sealed class RaftNodeBuilder
{
    private ClusterConfiguration? _cluster;
    private ILogStore? _logStore;
    private IStateStore? _stateStore;
    private ISnapshotStore? _snapshotStore;
    private IRaftTransport? _transport;
    private IRaftStateMachine? _stateMachine;
    private RaftOptions _options = new();
    private IRaftClock? _clock;
    private IElectionTimeoutJitter? _electionJitter;
    private IRaftLogger? _logger;
    private readonly List<IRaftObserver> _observers = new();

    public RaftNodeBuilder WithCluster(ClusterConfiguration cluster)
    {
        _cluster = cluster;
        return this;
    }

    public RaftNodeBuilder WithLogStore(ILogStore logStore)
    {
        _logStore = logStore;
        return this;
    }

    public RaftNodeBuilder WithStateStore(IStateStore stateStore)
    {
        _stateStore = stateStore;
        return this;
    }

    public RaftNodeBuilder WithSnapshotStore(ISnapshotStore snapshotStore)
    {
        _snapshotStore = snapshotStore;
        return this;
    }

    public RaftNodeBuilder WithTransport(IRaftTransport transport)
    {
        _transport = transport;
        return this;
    }

    public RaftNodeBuilder WithStateMachine(IRaftStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
        return this;
    }

    public RaftNodeBuilder WithOptions(RaftOptions options)
    {
        _options = options;
        return this;
    }

    public RaftNodeBuilder WithClock(IRaftClock clock)
    {
        _clock = clock;
        return this;
    }

    public RaftNodeBuilder WithElectionJitter(IElectionTimeoutJitter electionJitter)
    {
        _electionJitter = electionJitter;
        return this;
    }

    public RaftNodeBuilder WithLogger(IRaftLogger logger)
    {
        _logger = logger;
        return this;
    }

    public RaftNodeBuilder AddObserver(IRaftObserver observer)
    {
        _observers.Add(observer);
        return this;
    }

    public IRaftNode Build()
    {
        if (_cluster is null)
            throw new InvalidOperationException("Cluster configuration is required.");

        _options.Validate();

        var log = _logStore ?? new InMemoryLogStore();
        var state = _stateStore ?? new InMemoryStateStore();
        var transport = _transport ?? throw new InvalidOperationException(
            "Transport is required. Use InProcessRaftTransport for tests or provide your own implementation.");
        var machine = _stateMachine ?? throw new InvalidOperationException(
            "State machine is required.");
        var clock = _clock ?? new SystemRaftClock();
        var logger = _logger ?? new NullRaftLogger();

        var engine = new RaftEngine(
            _cluster,
            _options,
            log,
            state,
            _snapshotStore,
            transport,
            machine,
            clock,
            logger,
            _electionJitter,
            _observers);

        return new RaftNode(engine);
    }
}
