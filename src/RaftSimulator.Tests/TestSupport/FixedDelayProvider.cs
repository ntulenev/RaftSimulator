using RaftSimulator.Abstractions;

namespace RaftSimulator.Tests.TestSupport;

internal sealed class FixedDelayProvider : IRaftDelayProvider
{
    public TimeSpan GetDelay(TimeSpan min, TimeSpan max) => min;
}
