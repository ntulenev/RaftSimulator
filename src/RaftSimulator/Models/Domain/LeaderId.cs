using System.Globalization;

namespace RaftSimulator.Models.Domain;

/// <summary>
/// Leader node identifier value object.
/// </summary>
/// <param name="Value">Leader identifier value.</param>
internal readonly record struct LeaderId(int Value) : IFormattable
{
    /// <inheritdoc />
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Value.ToString(format, formatProvider);

    /// <summary>
    /// Converts an integer to a leader identifier value object.
    /// </summary>
    /// <param name="value">Integer value.</param>
    public static implicit operator LeaderId(int value) => new(value);
}
