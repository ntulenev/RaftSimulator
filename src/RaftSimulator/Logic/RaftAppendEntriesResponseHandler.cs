using RaftSimulator.Models.Domain;
using RaftSimulator.Models.Domain.Events;

namespace RaftSimulator.Logic;

/// <summary>
/// Append-entries response transitions for the raft state machine.
/// </summary>
internal sealed class RaftAppendEntriesResponseHandler
{
    internal RaftAppendEntriesResponseHandler(RaftStateMachineContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
    }

    internal AppendEntriesResponseDecision Handle(
        RaftAppendEntriesResponse response,
        DateTimeOffset now,
        TimeSpan electionTimeout)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (!response.HasHigherTermThan(_context.State.CurrentTerm))
        {
            return new AppendEntriesResponseDecision([]);
        }

        return new AppendEntriesResponseDecision(
        [
            new HigherTermDiscoveredEvent(response.Term, response.FromId),
            _context.BecomeFollower(response.Term.Value, null, now, electionTimeout)
        ]);
    }

    private readonly RaftStateMachineContext _context;
}
