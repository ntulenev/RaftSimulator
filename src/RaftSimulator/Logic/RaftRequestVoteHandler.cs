using RaftSimulator.Models.Domain;
using RaftSimulator.Models.Domain.Events;

namespace RaftSimulator.Logic;

/// <summary>
/// Request-vote transitions for the raft state machine.
/// </summary>
internal sealed class RaftRequestVoteHandler
{
    internal RaftRequestVoteHandler(RaftStateMachineContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
    }

    internal VoteDecision Handle(
        RaftVoteRequest request,
        DateTimeOffset now,
        TimeSpan electionTimeout)
    {
        ArgumentNullException.ThrowIfNull(request);

        var events = new List<RaftEvent>(2);

        if (request.IsStaleFor(_context.State.CurrentTerm))
        {
            events.Add(RaftStateMachineContext.CreateVoteEvent(request, granted: false, request.Term));
            return _context.CreateVoteDecision(granted: false, events);
        }

        if (request.AdvancesTerm(_context.State.CurrentTerm))
        {
            events.Add(_context.BecomeFollower(request.Term.Value, null, now, electionTimeout));
        }

        var canVote = _context.State.TryGrantVote(request, now, electionTimeout);
        events.Add(RaftStateMachineContext.CreateVoteEvent(request, canVote, _context.State.CurrentTerm));

        return _context.CreateVoteDecision(canVote, events);
    }

    private readonly RaftStateMachineContext _context;
}
