using System.Globalization;

namespace RaftSimulator.Models.Domain;

/// <summary>
/// Leader node identifier value object.
/// </summary>
internal readonly record struct LeaderId(int Value) : IFormattable
{
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Value.ToString(format, formatProvider);

    public static implicit operator int(LeaderId value) => value.Value;

    public static implicit operator LeaderId(int value) => new(value);
}
