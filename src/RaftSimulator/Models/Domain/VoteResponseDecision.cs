using RaftSimulator.Models.Domain.Events;

namespace RaftSimulator.Models.Domain;

/// <summary>
/// Result of handling a vote response from a peer.
/// </summary>
internal sealed record VoteResponseDecision
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VoteResponseDecision"/> class.
    /// </summary>
    /// <param name="events">Events emitted by the decision.</param>
    /// <param name="becameLeader">Value indicating whether the node became leader.</param>
    /// <param name="term">Term associated with follow-up work.</param>
    /// <param name="statusSnapshot">Optional status snapshot to publish.</param>
    public VoteResponseDecision(
        IReadOnlyList<RaftEvent> events,
        bool becameLeader,
        Term term,
        RaftStatus? statusSnapshot)
    {
        ArgumentNullException.ThrowIfNull(term);

        Events = RaftEventListGuard.RequireValid(events, nameof(events));
        BecameLeader = becameLeader;
        Term = term;
        StatusSnapshot = statusSnapshot;
    }

    /// <summary>
    /// Gets events emitted by the decision.
    /// </summary>
    public IReadOnlyList<RaftEvent> Events { get; }

    /// <summary>
    /// Gets a value indicating whether the node became leader.
    /// </summary>
    public bool BecameLeader { get; }

    /// <summary>
    /// Gets term associated with follow-up work.
    /// </summary>
    public Term Term { get; }

    /// <summary>
    /// Gets optional status snapshot to publish.
    /// </summary>
    public RaftStatus? StatusSnapshot { get; }
}
