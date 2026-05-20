using RaftSimulator.Models.Domain;
using RaftSimulator.Models.Domain.Events;

namespace RaftSimulator.Logic;

/// <summary>
/// Append-entries transitions for the raft state machine.
/// </summary>
internal sealed class RaftAppendEntriesHandler
{
    internal RaftAppendEntriesHandler(RaftStateMachineContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
    }

    internal AppendEntriesDecision Handle(
        RaftAppendEntriesRequest request,
        DateTimeOffset now,
        TimeSpan electionTimeout)
    {
        ArgumentNullException.ThrowIfNull(request);

        var events = new List<RaftEvent>(2);

        if (request.IsStaleFor(_context.State.CurrentTerm))
        {
            events.Add(new HeartbeatIgnoredEvent(request.LeaderId, request.Term));
            return _context.CreateAppendEntriesDecision(success: false, events, statusSnapshot: null);
        }

        if (request.ShouldMakeFollower(_context.State.CurrentTerm, _context.State.Role))
        {
            events.Add(_context.BecomeFollower(
                request.Term.Value,
                request.LeaderId.Value,
                now,
                electionTimeout));
        }

        _context.State.AcceptHeartbeat(request, now, electionTimeout);

        events.Add(new HeartbeatReceivedEvent(request.LeaderId, _context.State.CurrentTerm));

        return _context.CreateAppendEntriesDecision(
            success: true,
            events,
            _context.StatusReporter.GetSnapshotToPublish(_context.GetStatus()));
    }

    private readonly RaftStateMachineContext _context;
}
