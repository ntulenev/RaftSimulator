using RaftSimulator.Abstractions;

namespace RaftSimulator.Logic;

/// <summary>
/// System clock implementation.
/// </summary>
internal sealed class SystemRaftClock : IRaftClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
