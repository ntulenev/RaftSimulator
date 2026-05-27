namespace RaftSimulator.Models.Domain.Events;

/// <summary>
/// Validates raft event collections.
/// </summary>
internal static class RaftEventListGuard
{
    /// <summary>
    /// Requires a non-null event list without null items.
    /// </summary>
    /// <param name="events">Events to validate.</param>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns>Validated events.</returns>
    public static IReadOnlyList<RaftEvent> RequireValid(
        IReadOnlyList<RaftEvent> events,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(events, parameterName);

        if (events.Any(static @event => @event is null))
        {
            throw new ArgumentException("Events must not contain null items.", parameterName);
        }

        return events;
    }
}
