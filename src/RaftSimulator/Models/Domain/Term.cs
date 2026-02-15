using System.Globalization;

namespace RaftSimulator.Models.Domain;

/// <summary>
/// Raft term value object.
/// </summary>
internal readonly record struct Term(int Value) : IFormattable
{
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Value.ToString(format, formatProvider);

    public static implicit operator int(Term value) => value.Value;

    public static implicit operator Term(int value) => new(value);
}
