using SparkRaftSharp.Models;
using SparkRaftSharp.Persistence;
using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Tests;

public class FilePersistenceTests
{
    [Fact]
    public void File_log_store_round_trips_entries()
    {
        var dir = Path.Combine(Path.GetTempPath(), "sparkraft-" + Guid.NewGuid());
        var store = new FileLogStore(dir);
        store.Append(new RaftEntry(new LogIndex(1), new RaftTerm(1), new byte[] { 9 }));

        var reloaded = new FileLogStore(dir);
        Assert.Equal(1, reloaded.LastIndex.Value);
        Assert.Equal(9, reloaded.GetEntry(new LogIndex(1))!.Data.Span[0]);
    }

    [Fact]
    public void File_state_store_persists_term()
    {
        var dir = Path.Combine(Path.GetTempPath(), "sparkraft-" + Guid.NewGuid());
        var store = new FileStateStore(dir);
        store.Save(new PersistentState { CurrentTerm = new RaftTerm(7) });

        var loaded = new FileStateStore(dir);
        Assert.Equal(7, loaded.Load().CurrentTerm.Value);
    }
}
