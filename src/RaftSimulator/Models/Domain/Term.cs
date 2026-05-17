using System.Globalization;

namespace RaftSimulator.Models.Domain;

/// <summary>
/// Raft term value object.
/// </summary>
internal sealed record Term : IFormattable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Term"/> class.
    /// </summary>
    /// <param name="value">Term value.</param>
    public Term(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Term must be >= 0.");
        }

        Value = value;
    }

    /// <summary>
    /// Gets initial raft term.
    /// </summary>
    public static Term Initial { get; } = new(0);

    /// <summary>
    /// Gets term value.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Gets the next term.
    /// </summary>
    /// <returns>Next term.</returns>
    public Term Next()
    {
        if (Value == int.MaxValue)
        {
            throw new InvalidOperationException("Term cannot advance past int.MaxValue.");
        }

        return new Term(Value + 1);
    }

    /// <summary>
    /// Determines whether this term is older than another term.
    /// </summary>
    /// <param name="other">Other term.</param>
    /// <returns>True when this term is older.</returns>
    public bool IsOlderThan(Term other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return Value < other.Value;
    }

    /// <summary>
    /// Determines whether this term is newer than another term.
    /// </summary>
    /// <param name="other">Other term.</param>
    /// <returns>True when this term is newer.</returns>
    public bool IsNewerThan(Term other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return Value > other.Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Value.ToString(format, formatProvider);

    /// <summary>
    /// Converts an integer to a term value object.
    /// </summary>
    /// <param name="value">Integer value.</param>
    public static implicit operator Term(int value) => new(value);
}
