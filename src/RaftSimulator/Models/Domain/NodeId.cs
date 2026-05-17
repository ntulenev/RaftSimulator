using System.Globalization;

namespace RaftSimulator.Models.Domain;

/// <summary>
/// Raft node identifier value object.
/// </summary>
internal sealed record NodeId : IFormattable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NodeId"/> class.
    /// </summary>
    /// <param name="value">Node identifier value.</param>
    public NodeId(int value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Node id must be >= 1.");
        }

        Value = value;
    }

    /// <summary>
    /// Gets node identifier value.
    /// </summary>
    public int Value { get; }

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
