using System.Globalization;

namespace RaftSimulator.Models.Domain;

/// <summary>
/// Raft node identifier value object.
/// </summary>
internal readonly record struct NodeId(int Value) : IFormattable
{
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Value.ToString(format, formatProvider);

    public static implicit operator int(NodeId value) => value.Value;

    public static implicit operator NodeId(int value) => new(value);
}
