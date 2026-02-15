using System.Globalization;

namespace RaftSimulator.Models.Domain;

/// <summary>
/// Candidate node identifier value object.
/// </summary>
internal readonly record struct CandidateId(int Value) : IFormattable
{
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Value.ToString(format, formatProvider);

    public static implicit operator int(CandidateId value) => value.Value;

    public static implicit operator CandidateId(int value) => new(value);
}
