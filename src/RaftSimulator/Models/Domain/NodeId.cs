using System.Globalization;

namespace RaftSimulator.Models.Domain;

/// <summary>
/// Raft node identifier value object.
/// </summary>
/// <param name="Value">Node identifier value.</param>
internal readonly record struct NodeId(int Value) : IFormattable
{
    /// <inheritdoc />
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Value.ToString(format, formatProvider);

    /// <summary>
    /// Converts an integer to a node identifier value object.
    /// </summary>
    /// <param name="value">Integer value.</param>
    public static implicit operator NodeId(int value) => new(value);
}
