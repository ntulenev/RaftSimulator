using RaftSimulator.Abstractions;

namespace RaftSimulator.Logic;

/// <summary>
/// Randomized raft delay provider.
/// </summary>
internal sealed class RaftDelayProvider : IRaftDelayProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RaftDelayProvider"/> class.
    /// </summary>
    /// <param name="random">Random source.</param>
    public RaftDelayProvider(IRaftRandom random)
    {
        ArgumentNullException.ThrowIfNull(random);

        _random = random;
    }

    /// <inheritdoc />
    public TimeSpan GetDelay(TimeSpan min, TimeSpan max)
    {
        if (min == max)
        {
            return min;
        }

        var window = max - min;
        var offset = TimeSpan.FromMilliseconds(window.TotalMilliseconds * _random.NextDouble());
        return min + offset;
    }

    private readonly IRaftRandom _random;
}
