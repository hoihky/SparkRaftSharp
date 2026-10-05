namespace SparkRaftSharp.Persistence;

public sealed class InMemorySnapshotStore : ISnapshotStore
{
    private RaftSnapshot? _latest;

    public RaftSnapshot? GetLatest() => _latest;

    public void Save(RaftSnapshot snapshot) => _latest = snapshot;
}
