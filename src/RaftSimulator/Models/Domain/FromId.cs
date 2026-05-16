using System.Globalization;

namespace RaftSimulator.Models.Domain;

/// <summary>
/// Response sender node identifier value object.
/// </summary>
/// <param name="Value">Sender identifier value.</param>
internal readonly record struct FromId(int Value) : IFormattable
{
    /// <inheritdoc />
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Value.ToString(format, formatProvider);

    /// <summary>
    /// Converts an integer to a sender identifier value object.
    /// </summary>
    /// <param name="value">Integer value.</param>
    public static implicit operator FromId(int value) => new(value);
}
