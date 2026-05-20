using RaftSimulator.Models.Domain;
using RaftSimulator.Models.Domain.Events;

namespace RaftSimulator.Logic;

/// <summary>
/// Append-entries response transitions for the raft state machine.
/// </summary>
internal sealed partial class RaftStateMachine
{
    /// <summary>
    /// Handles an append-entries response from a peer.
    /// </summary>
    /// <param name="response">Append entries response.</param>
    /// <param name="now">Current time.</param>
    /// <param name="electionTimeout">Next election timeout.</param>
    /// <returns>Append entries response decision.</returns>
    public AppendEntriesResponseDecision HandleAppendEntriesResponse(
        RaftAppendEntriesResponse response,
        DateTimeOffset now,
        TimeSpan electionTimeout)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (!response.HasHigherTermThan(State.CurrentTerm))
        {
            return new AppendEntriesResponseDecision([]);
        }

        return new AppendEntriesResponseDecision(
        [
            new HigherTermDiscoveredEvent(response.Term, response.FromId),
            BecomeFollower(response.Term.Value, null, now, electionTimeout)
        ]);
    }
}
