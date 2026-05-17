using System.Globalization;

namespace RaftSimulator.Models.Domain;

/// <summary>
/// Candidate node identifier value object.
/// </summary>
internal sealed record CandidateId : IFormattable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CandidateId"/> class.
    /// </summary>
    /// <param name="value">Candidate identifier value.</param>
    public CandidateId(int value)
    {
        Value = DomainValueGuard.RequirePositiveId(value, nameof(value), "Candidate id");
    }

    /// <summary>
    /// Gets candidate identifier value.
    /// </summary>
    public int Value { get; }

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
