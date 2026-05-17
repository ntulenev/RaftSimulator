using RaftSimulator.Models.Domain.Events;

namespace RaftSimulator.Models.Domain;

/// <summary>
/// Result of handling an append entries request.
/// </summary>
internal sealed record AppendEntriesDecision
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppendEntriesDecision"/> class.
    /// </summary>
    /// <param name="response">Append entries response to return.</param>
    /// <param name="events">Events emitted by the decision.</param>
    /// <param name="statusSnapshot">Optional status snapshot to publish.</param>
    public AppendEntriesDecision(
        RaftAppendEntriesResponse response,
        IReadOnlyList<RaftEvent> events,
        RaftStatus? statusSnapshot)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(events);

        if (events.Any(static @event => @event is null))
        {
            throw new ArgumentException("Events must not contain null items.", nameof(events));
        }

        Response = response;
        Events = events;
        StatusSnapshot = statusSnapshot;
    }

    /// <summary>
    /// Gets append entries response to return.
    /// </summary>
    public RaftAppendEntriesResponse Response { get; }

    /// <summary>
    /// Gets events emitted by the decision.
    /// </summary>
    public IReadOnlyList<RaftEvent> Events { get; }

    /// <summary>
    /// Gets optional status snapshot to publish.
    /// </summary>
    public RaftStatus? StatusSnapshot { get; }
}
