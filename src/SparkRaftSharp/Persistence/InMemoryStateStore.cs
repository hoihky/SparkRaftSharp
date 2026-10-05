namespace SparkRaftSharp.Persistence;

public sealed class InMemoryStateStore : IStateStore
{
    private PersistentState _state = new();

    public PersistentState Load() => _state;

    public void Save(PersistentState state) => _state = state;
}
