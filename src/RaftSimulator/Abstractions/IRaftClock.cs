namespace RaftSimulator.Abstractions;

/// <summary>
/// Provides current time for raft scheduling.
/// </summary>
internal interface IRaftClock
{
    /// <summary>
    /// Gets current UTC time.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}
