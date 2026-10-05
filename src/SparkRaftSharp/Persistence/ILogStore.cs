using SparkRaftSharp.Models;
using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Persistence;

public interface ILogStore
{
    LogIndex LastIndex { get; }

    RaftTerm GetTerm(LogIndex index);

    RaftEntry? GetEntry(LogIndex index);

    void Append(RaftEntry entry);

    void DeleteSuffixFrom(LogIndex fromIndexInclusive);
}
