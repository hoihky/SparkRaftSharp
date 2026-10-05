using SparkRaftSharp.Messaging;
using SparkRaftSharp.Models;
using SparkRaftSharp.Primitives;

namespace SparkRaftSharp.Internal.Services;

internal sealed class ElectionService
{
    private readonly RaftRuntime _runtime;
    private readonly RaftObserverNotifier _notifier;
    private readonly Action _onBecameLeader;

    public ElectionService(RaftRuntime runtime, RaftObserverNotifier notifier, Action onBecameLeader)
    {
        _runtime = runtime;
        _notifier = notifier;
        _onBecameLeader = onBecameLeader;
    }

    public void OnElectionTimeout()
    {
        if (_runtime.Role == RaftRole.Leader)
            return;

        StartElection();
    }

    public void StartElection()
    {
        _runtime.Role = RaftRole.Candidate;
        _runtime.LeaderId = null;
        _runtime.VotesReceived = 1;
        _runtime.State.CurrentTerm = _runtime.State.CurrentTerm.Next;
        _runtime.State.VotedFor = _runtime.Cluster.LocalNodeId;
        _runtime.PersistState();

        _runtime.Logger.Info(
            $"Node {_runtime.Cluster.LocalNodeId} started election for term {_runtime.State.CurrentTerm.Value}");
        _notifier.RoleChanged(_runtime.Role, _runtime.State.CurrentTerm);

        foreach (var peer in _runtime.Cluster.RemotePeers)
        {
            var request = new RequestVoteRequest(
                _runtime.Cluster.LocalNodeId,
                _runtime.State.CurrentTerm,
                _runtime.Cluster.LocalNodeId,
                _runtime.Log.LastIndex,
                _runtime.GetLastLogTerm());

            _ = _runtime.Transport.SendAsync(peer.Id, request);
        }
    }

    public void HandleRequestVote(RequestVoteRequest request, Action scheduleElectionTimeout)
    {
        if (request.Term.Value > _runtime.State.CurrentTerm.Value)
            BecomeFollower(request.Term, null, scheduleElectionTimeout);

        if (request.Term.Value < _runtime.State.CurrentTerm.Value)
        {
            SendVoteResponse(request.SenderId, _runtime.State.CurrentTerm, false);
            return;
        }

        var grant = false;
        if (_runtime.IsLogUpToDate(request.LastLogIndex, request.LastLogTerm) &&
            (_runtime.State.VotedFor is null || _runtime.State.VotedFor == request.CandidateId))
        {
            _runtime.State.VotedFor = request.CandidateId;
            _runtime.PersistState();
            grant = true;
            scheduleElectionTimeout();
        }

        SendVoteResponse(request.SenderId, _runtime.State.CurrentTerm, grant);
    }

    public void HandleRequestVoteResponse(RequestVoteResponse response, Action becomeFollower)
    {
        if (response.Term.Value > _runtime.State.CurrentTerm.Value)
        {
            becomeFollower();
            return;
        }

        if (_runtime.Role != RaftRole.Candidate || response.Term != _runtime.State.CurrentTerm)
            return;

        if (!response.VoteGranted)
            return;

        _runtime.VotesReceived++;
        if (_runtime.VotesReceived >= _runtime.Cluster.QuorumSize)
            _onBecameLeader();
    }

    public void BecomeFollower(RaftTerm term, NodeId? leaderId, Action scheduleElectionTimeout)
    {
        var termChanged = term.Value > _runtime.State.CurrentTerm.Value;
        if (termChanged)
        {
            _runtime.State.CurrentTerm = term;
            _runtime.State.VotedFor = null;
            _runtime.PersistState();
        }

        _runtime.Role = RaftRole.Follower;
        _runtime.LeaderId = leaderId;
        _runtime.VotesReceived = 0;
        _notifier.RoleChanged(_runtime.Role, _runtime.State.CurrentTerm);
        if (leaderId is not null)
            _notifier.LeaderChanged(leaderId, _runtime.State.CurrentTerm);
        scheduleElectionTimeout();
    }

    private void SendVoteResponse(NodeId target, RaftTerm term, bool granted)
    {
        var response = new RequestVoteResponse(_runtime.Cluster.LocalNodeId, term, granted);
        _ = _runtime.Transport.SendAsync(target, response);
    }
}
