using System.Globalization;

namespace RaftSimulator.Models.Domain;

/// <summary>
/// Response sender node identifier value object.
/// </summary>
internal readonly record struct FromId(int Value) : IFormattable
{
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Value.ToString(format, formatProvider);

    public static implicit operator int(FromId value) => value.Value;

    public static implicit operator FromId(int value) => new(value);
}
