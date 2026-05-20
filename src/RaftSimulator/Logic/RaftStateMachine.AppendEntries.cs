using RaftSimulator.Models.Domain;
using RaftSimulator.Models.Domain.Events;

namespace RaftSimulator.Logic;

/// <summary>
/// Append-entries transitions for the raft state machine.
/// </summary>
internal sealed partial class RaftStateMachine
{
    /// <summary>
    /// Handles an append-entries RPC.
    /// </summary>
    /// <param name="request">Append entries request.</param>
    /// <param name="now">Current time.</param>
    /// <param name="electionTimeout">Next election timeout.</param>
    /// <returns>Append entries decision.</returns>
    public AppendEntriesDecision HandleAppendEntries(
        RaftAppendEntriesRequest request,
        DateTimeOffset now,
        TimeSpan electionTimeout)
    {
        ArgumentNullException.ThrowIfNull(request);

        var events = new List<RaftEvent>(2);

        if (request.IsStaleFor(State.CurrentTerm))
        {
            events.Add(new HeartbeatIgnoredEvent(request.LeaderId, request.Term));
            return CreateAppendEntriesDecision(success: false, events, statusSnapshot: null);
        }

        if (request.ShouldMakeFollower(State.CurrentTerm, State.Role))
        {
            events.Add(BecomeFollower(
                request.Term.Value,
                request.LeaderId.Value,
                now,
                electionTimeout));
        }

        State.AcceptHeartbeat(request, now, electionTimeout);

        events.Add(new HeartbeatReceivedEvent(request.LeaderId, State.CurrentTerm));

        return CreateAppendEntriesDecision(
            success: true,
            events,
            _statusReporter.GetSnapshotToPublish(GetStatus()));
    }
}
