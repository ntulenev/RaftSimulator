namespace RaftSimulator.Models.Domain;

/// <summary>
/// Validates primitive values before they enter domain value objects.
/// </summary>
internal static class DomainValueGuard
{
    /// <summary>
    /// Requires a positive identifier value.
    /// </summary>
    /// <param name="value">Identifier value.</param>
    /// <param name="parameterName">Parameter name.</param>
    /// <param name="displayName">Display name.</param>
    /// <returns>Validated value.</returns>
    public static int RequirePositiveId(
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

    /// <summary>
    /// Requires a non-negative term value.
    /// </summary>
    /// <param name="value">Term value.</param>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns>Validated value.</returns>
    public static int RequireNonNegativeTerm(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Term must be >= 0.");
        }

        return value;
    }
}
