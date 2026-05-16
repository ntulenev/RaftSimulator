using System.Globalization;

namespace RaftSimulator.Models.Domain;

/// <summary>
/// Raft term value object.
/// </summary>
/// <param name="Value">Term value.</param>
internal readonly record struct Term(int Value) : IFormattable
{
    /// <inheritdoc />
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Value.ToString(format, formatProvider);

    /// <summary>
    /// Converts a term to its integer value.
    /// </summary>
    /// <param name="value">Term value object.</param>
    public static implicit operator int(Term value) => value.Value;

    /// <summary>
    /// Converts an integer to a term value object.
    /// </summary>
    /// <param name="value">Integer value.</param>
    public static implicit operator Term(int value) => new(value);
}
