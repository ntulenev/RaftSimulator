using RaftSimulator.Models.Configuration;

namespace RaftSimulator.Logic;

/// <summary>
/// Reports leader quorum health based on heartbeat acknowledgement freshness.
/// </summary>
internal sealed class RaftQuorumReporter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RaftQuorumReporter"/> class.
    /// </summary>
    /// <param name="settings">Raft settings.</param>
    /// <param name="coordinator">Node state coordinator.</param>
    /// <param name="publisher">Decision publisher.</param>
    public RaftQuorumReporter(
        RaftSettings settings,
        RaftNodeCoordinator coordinator,
        RaftDecisionPublisher publisher)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(coordinator);
        ArgumentNullException.ThrowIfNull(publisher);

        _settings = settings;
        _coordinator = coordinator;
        _publisher = publisher;
    }

    /// <summary>
    /// Reports out-of-quorum when the current leader cannot reach majority.
    /// </summary>
    public void Report()
    {
        var quorumEvent = _coordinator.BuildQuorumEvent(GetQuorumWindow());
        if (quorumEvent is not null)
        {
            _publisher.Publish(quorumEvent);
        }
    }

    private TimeSpan GetQuorumWindow() =>
        _settings.HeartbeatInterval + _settings.MaxNetworkDelay;

    private readonly RaftSettings _settings;
    private readonly RaftNodeCoordinator _coordinator;
    private readonly RaftDecisionPublisher _publisher;
}
