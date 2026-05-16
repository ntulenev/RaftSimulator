using System.Globalization;

namespace RaftSimulator.Models.Domain;

/// <summary>
/// Candidate node identifier value object.
/// </summary>
/// <param name="Value">Candidate identifier value.</param>
internal readonly record struct CandidateId(int Value) : IFormattable
{
    /// <inheritdoc />
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Value.ToString(format, formatProvider);

    /// <summary>
    /// Converts an integer to a candidate identifier value object.
    /// </summary>
    /// <param name="value">Integer value.</param>
    public static implicit operator CandidateId(int value) => new(value);
}
