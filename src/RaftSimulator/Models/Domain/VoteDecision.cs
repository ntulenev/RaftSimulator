using RaftSimulator.Models.Domain.Events;

namespace RaftSimulator.Models.Domain;

/// <summary>
/// Result of handling a vote request.
/// </summary>
internal sealed record VoteDecision
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VoteDecision"/> class.
    /// </summary>
    /// <param name="response">Vote response to return.</param>
    /// <param name="events">Events emitted by the decision.</param>
    public VoteDecision(RaftVoteResponse response, IReadOnlyList<RaftEvent> events)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(events);

        if (events.Any(static @event => @event is null))
        {
            throw new ArgumentException("Events must not contain null items.", nameof(events));
        }

        Response = response;
        Events = events;
    }

    /// <summary>
    /// Gets vote response to return.
    /// </summary>
    public RaftVoteResponse Response { get; }

    /// <summary>
    /// Gets events emitted by the decision.
    /// </summary>
    public IReadOnlyList<RaftEvent> Events { get; }
}
