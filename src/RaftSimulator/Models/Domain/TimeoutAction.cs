using RaftSimulator.Models.Domain.Events;

namespace RaftSimulator.Models.Domain;

/// <summary>
/// Describes work to run after a raft timeout.
/// </summary>
internal sealed record TimeoutAction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeoutAction"/> class.
    /// </summary>
    /// <param name="type">Timeout action type.</param>
    /// <param name="term">Term associated with the action.</param>
    /// <param name="events">Events emitted by the action.</param>
    public TimeoutAction(
        TimeoutActionType type,
        Term term,
        IReadOnlyList<RaftEvent> events)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Timeout action type is not supported.");
        }

        ArgumentNullException.ThrowIfNull(term);
        ArgumentNullException.ThrowIfNull(events);

        if (events.Any(static @event => @event is null))
        {
            throw new ArgumentException("Events must not contain null items.", nameof(events));
        }

        Type = type;
        Term = term;
        Events = events;
    }

    /// <summary>
    /// Gets timeout action type.
    /// </summary>
    public TimeoutActionType Type { get; }

    /// <summary>
    /// Gets term associated with the action.
    /// </summary>
    public Term Term { get; }

    /// <summary>
    /// Gets events emitted by the action.
    /// </summary>
    public IReadOnlyList<RaftEvent> Events { get; }
}
