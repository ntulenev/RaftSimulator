using RaftSimulator.Models.Domain;
using RaftSimulator.Models.Domain.Events;

namespace RaftSimulator.Logic;

/// <summary>
/// Vote response transitions for the raft state machine.
/// </summary>
internal sealed class RaftVoteResponseHandler
{
    internal RaftVoteResponseHandler(RaftStateMachineContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
    }

    internal VoteResponseDecision Handle(
        RaftVoteResponse response,
        DateTimeOffset now,
        TimeSpan electionTimeout)
    {
        ArgumentNullException.ThrowIfNull(response);

        var events = new List<RaftEvent>(2);
        var becameLeader = false;
        var term = response.Term;
        RaftStatus? statusSnapshot = null;

        if (response.HasHigherTermThan(_context.State.CurrentTerm))
        {
            events.Add(new HigherTermDiscoveredEvent(response.Term, response.FromId));
            events.Add(_context.BecomeFollower(response.Term.Value, null, now, electionTimeout));
        }
        else if (_context.State.Role != RaftRole.Candidate || !response.IsForTerm(_context.State.CurrentTerm))
        {
            return RaftStateMachineContext.CreateVoteResponseDecision(
                [],
                becameLeader: false,
                term,
                statusSnapshot: null);
        }
        else if (!response.Granted)
        {
            events.Add(new VoteResponseDeniedEvent(response.FromId, _context.State.CurrentTerm));
        }
        else
        {
            var votesReceived = _context.State.RecordGrantedVote();
            events.Add(new VoteResponseGrantedEvent(
                response.FromId,
                votesReceived,
                _context.Settings.Majority));

            if (_context.State.HasMajority(_context.Settings.Majority))
            {
                _context.State.BecomeLeader(_context.Id, _context.Settings.Majority, now);
                events.Add(new BecameLeaderEvent(_context.State.CurrentTerm));
                becameLeader = true;
                term = _context.State.CurrentTerm;

                statusSnapshot = _context.StatusReporter.GetSnapshotToPublish(_context.GetStatus());
            }
        }

        return RaftStateMachineContext.CreateVoteResponseDecision(events, becameLeader, term, statusSnapshot);
    }

    private readonly RaftStateMachineContext _context;
}
