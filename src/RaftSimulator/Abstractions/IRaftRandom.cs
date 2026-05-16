namespace RaftSimulator.Abstractions;

/// <summary>
/// Provides random values for raft timing.
/// </summary>
internal interface IRaftRandom
{
    /// <summary>
    /// Returns a random value in the [0, 1) range.
    /// </summary>
    /// <returns>Random value.</returns>
    double NextDouble();
}
