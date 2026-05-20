using RaftSimulator.Models.Domain;
using RaftSimulator.Models.Domain.Events;

namespace RaftSimulator.Logic;

/// <summary>
/// Timeout transitions for the raft state machine.
/// </summary>
internal sealed class RaftTimeoutHandler
{
    internal RaftTimeoutHandler(RaftStateMachineContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
    }

    internal TimeoutAction Prepare(
        DateTimeOffset now,
        TimeSpan electionTimeout,
        TimeSpan heartbeatInterval)
    {
        if (_context.State.Role == RaftRole.Leader)
        {
            _context.State.ScheduleHeartbeat(now, heartbeatInterval);
            return new TimeoutAction(
                TimeoutActionType.Heartbeats,
                _context.State.CurrentTerm,
                [new LeaderHeartbeatEvent()]);
        }

        _context.State.StartElection(_context.Id, now, electionTimeout);

        return new TimeoutAction(
            TimeoutActionType.Election,
            _context.State.CurrentTerm,
            [new ElectionTimeoutEvent(_context.State.CurrentTerm)]);
    }

    private readonly RaftStateMachineContext _context;
}
