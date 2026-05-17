using System.Globalization;

namespace RaftSimulator.Models.Domain;

/// <summary>
/// Leader node identifier value object.
/// </summary>
internal sealed record LeaderId : IFormattable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LeaderId"/> class.
    /// </summary>
    /// <param name="value">Leader identifier value.</param>
    public LeaderId(int value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Leader id must be >= 1.");
        }

        Value = value;
    }

    /// <summary>
    /// Gets leader identifier value.
    /// </summary>
    public int Value { get; }

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
