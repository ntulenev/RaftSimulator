using RaftSimulator.Models.Domain;
using RaftSimulator.Models.Domain.Events;

namespace RaftSimulator.Logic;

/// <summary>
/// Request-vote transitions for the raft state machine.
/// </summary>
internal sealed partial class RaftStateMachine
{
    /// <summary>
    /// Handles a request-vote RPC.
    /// </summary>
    /// <param name="request">Vote request.</param>
    /// <param name="now">Current time.</param>
    /// <param name="electionTimeout">Next election timeout.</param>
    /// <returns>Vote decision.</returns>
    public VoteDecision HandleRequestVote(
        RaftVoteRequest request,
        DateTimeOffset now,
        TimeSpan electionTimeout)
    {
        ArgumentNullException.ThrowIfNull(request);

        var events = new List<RaftEvent>(2);

        if (request.IsStaleFor(State.CurrentTerm))
        {
            events.Add(CreateVoteEvent(request, granted: false, request.Term));
            return CreateVoteDecision(granted: false, events);
        }

        if (request.AdvancesTerm(State.CurrentTerm))
        {
            events.Add(BecomeFollower(request.Term.Value, null, now, electionTimeout));
        }

        var canVote = State.TryGrantVote(request, now, electionTimeout);
        events.Add(CreateVoteEvent(request, canVote, State.CurrentTerm));

        return CreateVoteDecision(canVote, events);
    }
}
