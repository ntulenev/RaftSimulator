using System.Globalization;

namespace RaftSimulator.Models.Domain;

/// <summary>
/// Response sender node identifier value object.
/// </summary>
internal sealed record FromId : IFormattable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FromId"/> class.
    /// </summary>
    /// <param name="value">Sender identifier value.</param>
    public FromId(int value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Sender id must be >= 1.");
        }

        Value = value;
    }

    /// <summary>
    /// Gets sender identifier value.
    /// </summary>
    public int Value { get; }

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
