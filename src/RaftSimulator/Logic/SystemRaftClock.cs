using RaftSimulator.Abstractions;

namespace RaftSimulator.Logic;

/// <summary>
/// System clock implementation.
/// </summary>
internal sealed class SystemRaftClock : IRaftClock
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SystemRaftClock"/> class.
    /// </summary>
    public SystemRaftClock()
    {
    }

    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
