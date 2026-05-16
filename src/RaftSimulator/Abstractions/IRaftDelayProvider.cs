namespace RaftSimulator.Abstractions;

/// <summary>
/// Provides randomized raft delays.
/// </summary>
internal interface IRaftDelayProvider
{
    /// <summary>
    /// Gets a delay within the configured range.
    /// </summary>
    /// <param name="min">Minimum delay.</param>
    /// <param name="max">Maximum delay.</param>
    /// <returns>Randomized delay.</returns>
    TimeSpan GetDelay(TimeSpan min, TimeSpan max);
}
