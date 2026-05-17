namespace RaftSimulator.Models.Domain.Events;

/// <summary>
/// Validates primitive values used by raft domain events.
/// </summary>
internal static class DomainEventGuard
{
    /// <summary>
    /// Requires a non-negative event term.
    /// </summary>
    /// <param name="value">Term value.</param>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns>Validated term.</returns>
    public static int RequireTerm(int value, string parameterName) =>
        DomainValueGuard.RequireNonNegativeTerm(value, parameterName);

    /// <summary>
    /// Requires a positive event node identifier.
    /// </summary>
    /// <param name="value">Node identifier value.</param>
    /// <param name="parameterName">Parameter name.</param>
    /// <param name="displayName">Display name.</param>
    /// <returns>Validated identifier.</returns>
    public static int RequireNodeId(
        int value,
        string parameterName,
        string displayName) =>
        DomainValueGuard.RequirePositiveId(value, parameterName, displayName);

    /// <summary>
    /// Requires a positive count value.
    /// </summary>
    /// <param name="value">Count value.</param>
    /// <param name="parameterName">Parameter name.</param>
    /// <param name="displayName">Display name.</param>
    /// <returns>Validated count.</returns>
    public static int RequirePositiveCount(
        int value,
        string parameterName,
        string displayName)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"{displayName} must be >= 1.");
        }

        return value;
    }
}
