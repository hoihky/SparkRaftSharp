namespace SparkRaftSharp.Persistence;

public interface ISnapshotStore
{
    RaftSnapshot? GetLatest();

    void Save(RaftSnapshot snapshot);
}
