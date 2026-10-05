namespace SparkRaftSharp.Persistence;

public interface IStateStore
{
    PersistentState Load();

    void Save(PersistentState state);
}
