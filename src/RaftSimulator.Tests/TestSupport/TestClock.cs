using RaftSimulator.Abstractions;

namespace RaftSimulator.Tests.TestSupport;

internal sealed class TestClock : IRaftClock
{
    public TestClock()
        : this(new DateTimeOffset(2026, 5, 17, 12, 0, 0, TimeSpan.Zero))
    {
    }

    public TestClock(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; private set; }

    public void Advance(TimeSpan value) =>
        UtcNow += value;
}
