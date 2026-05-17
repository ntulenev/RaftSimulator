using RaftSimulator.Logic.Events;

namespace RaftSimulator.Models.Domain;

/// <summary>
/// Result of handling an append entries response from a peer.
/// </summary>
internal sealed record AppendEntriesResponseDecision
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppendEntriesResponseDecision"/> class.
    /// </summary>
    /// <param name="events">Events emitted by the decision.</param>
    public AppendEntriesResponseDecision(IReadOnlyList<RaftEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        if (events.Any(static @event => @event is null))
        {
            throw new ArgumentException("Events must not contain null items.", nameof(events));
        }

        Events = events;
    }

    /// <summary>
    /// Gets events emitted by the decision.
    /// </summary>
    public IReadOnlyList<RaftEvent> Events { get; }
}
