using RaftSimulator.Models.Domain;
using RaftSimulator.Models.Domain.Events;

namespace RaftSimulator.Logic;

/// <summary>
/// Timeout transitions for the raft state machine.
/// </summary>
internal sealed partial class RaftStateMachine
{
    /// <summary>
    /// Prepares work for the next elapsed timeout.
    /// </summary>
    /// <param name="now">Current time.</param>
    /// <param name="electionTimeout">Next election timeout.</param>
    /// <param name="heartbeatInterval">Heartbeat interval.</param>
    /// <returns>Timeout action.</returns>
    public TimeoutAction PrepareTimeoutAction(
        DateTimeOffset now,
        TimeSpan electionTimeout,
        TimeSpan heartbeatInterval)
    {
        if (State.Role == RaftRole.Leader)
        {
            State.ScheduleHeartbeat(now, heartbeatInterval);
            return new TimeoutAction(
                TimeoutActionType.Heartbeats,
                State.CurrentTerm,
                [new LeaderHeartbeatEvent()]);
        }

        State.StartElection(Id, now, electionTimeout);

        return new TimeoutAction(
            TimeoutActionType.Election,
            State.CurrentTerm,
            [new ElectionTimeoutEvent(State.CurrentTerm)]);
    }
}
