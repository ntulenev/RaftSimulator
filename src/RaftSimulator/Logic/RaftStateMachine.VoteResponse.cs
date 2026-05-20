using RaftSimulator.Models.Domain;
using RaftSimulator.Models.Domain.Events;

namespace RaftSimulator.Logic;

/// <summary>
/// Vote response transitions for the raft state machine.
/// </summary>
internal sealed partial class RaftStateMachine
{
    /// <summary>
    /// Handles a vote response from a peer.
    /// </summary>
    /// <param name="response">Vote response.</param>
    /// <param name="now">Current time.</param>
    /// <param name="electionTimeout">Next election timeout.</param>
    /// <returns>Vote response decision.</returns>
    public VoteResponseDecision HandleVoteResponse(
        RaftVoteResponse response,
        DateTimeOffset now,
        TimeSpan electionTimeout)
    {
        ArgumentNullException.ThrowIfNull(response);

        var events = new List<RaftEvent>(2);
        var becameLeader = false;
        var term = response.Term;
        RaftStatus? statusSnapshot = null;

        if (response.HasHigherTermThan(State.CurrentTerm))
        {
            events.Add(new HigherTermDiscoveredEvent(response.Term, response.FromId));
            events.Add(BecomeFollower(response.Term.Value, null, now, electionTimeout));
        }
        else if (State.Role != RaftRole.Candidate || !response.IsForTerm(State.CurrentTerm))
        {
            return CreateVoteResponseDecision([], becameLeader: false, term, statusSnapshot: null);
        }
        else if (!response.Granted)
        {
            events.Add(new VoteResponseDeniedEvent(response.FromId, State.CurrentTerm));
        }
        else
        {
            var votesReceived = State.RecordGrantedVote();
            events.Add(new VoteResponseGrantedEvent(
                response.FromId,
                votesReceived,
                _settings.Majority));

            if (State.HasMajority(_settings.Majority))
            {
                State.BecomeLeader(Id, _settings.Majority, now);
                events.Add(new BecameLeaderEvent(State.CurrentTerm));
                becameLeader = true;
                term = State.CurrentTerm;

                statusSnapshot = _statusReporter.GetSnapshotToPublish(GetStatus());
            }
        }

        return CreateVoteResponseDecision(events, becameLeader, term, statusSnapshot);
    }
}
