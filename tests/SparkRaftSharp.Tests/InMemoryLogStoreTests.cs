using SparkRaftSharp.Models;
using SparkRaftSharp.Persistence;
using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Tests;

public class InMemoryLogStoreTests
{
    [Fact]
    public void Append_is_sequential()
    {
        var store = new InMemoryLogStore();
        store.Append(new RaftEntry(new LogIndex(1), new RaftTerm(1), new byte[] { 1 }));
        store.Append(new RaftEntry(new LogIndex(2), new RaftTerm(1), new byte[] { 2 }));

        Assert.Equal(2, store.LastIndex.Value);
        Assert.Equal(new RaftTerm(1), store.GetTerm(new LogIndex(2)));
    }

    [Fact]
    public void DeleteSuffix_removes_tail()
    {
        var store = new InMemoryLogStore();
        store.Append(new RaftEntry(new LogIndex(1), new RaftTerm(1), new byte[] { 1 }));
        store.Append(new RaftEntry(new LogIndex(2), new RaftTerm(2), new byte[] { 2 }));

        store.DeleteSuffixFrom(new LogIndex(2));

        Assert.Equal(1, store.LastIndex.Value);
        Assert.Null(store.GetEntry(new LogIndex(2)));
    }
}
