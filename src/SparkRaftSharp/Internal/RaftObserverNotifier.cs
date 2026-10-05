using SparkRaftSharp.Models;
using SparkRaftSharp.Observers;
using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Internal;

internal sealed class RaftObserverNotifier
{
    private readonly IReadOnlyList<IRaftObserver> _observers;

    public RaftObserverNotifier(IReadOnlyList<IRaftObserver> observers) =>
        _observers = observers;

    public void RoleChanged(RaftRole role, RaftTerm term)
    {
        foreach (var observer in _observers)
            observer.OnRoleChanged(role, term);
    }

    public void LeaderChanged(NodeId? leaderId, RaftTerm term)
    {
        foreach (var observer in _observers)
            observer.OnLeaderChanged(leaderId, term);
    }

    public void EntryCommitted(LogIndex index, RaftTerm term)
    {
        foreach (var observer in _observers)
            observer.OnEntryCommitted(index, term);
    }

    public void EntryApplied(LogIndex index)
    {
        foreach (var observer in _observers)
            observer.OnEntryApplied(index);
    }
}
