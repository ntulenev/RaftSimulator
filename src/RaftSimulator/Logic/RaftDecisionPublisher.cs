using RaftSimulator.Abstractions;
using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;
using RaftSimulator.Models.Domain.Events;

namespace RaftSimulator.Logic;

/// <summary>
/// Publishes raft decisions to the configured log sinks.
/// </summary>
internal sealed class RaftDecisionPublisher
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RaftDecisionPublisher"/> class.
    /// </summary>
    /// <param name="settings">Raft settings.</param>
    /// <param name="log">Log sink.</param>
    /// <param name="eventLog">Event log sink.</param>
    public RaftDecisionPublisher(
        RaftSettings settings,
        IRaftLog log,
        IRaftEventLog eventLog)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(eventLog);

        _settings = settings;
        _log = log;
        _eventLog = eventLog;
    }

    /// <summary>
    /// Publishes a timeout action.
    /// </summary>
    /// <param name="action">Timeout action.</param>
    public void Publish(TimeoutAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        LogEvents(action.Events);
    }

    /// <summary>
    /// Publishes a vote decision.
    /// </summary>
    /// <param name="decision">Vote decision.</param>
    public void Publish(VoteDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);

        LogEvents(decision.Events);
    }

    /// <summary>
    /// Publishes an append-entries decision.
    /// </summary>
    /// <param name="decision">Append-entries decision.</param>
    public void Publish(AppendEntriesDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);

        LogEvents(decision.Events);
        PublishStatus(decision.StatusSnapshot);
    }

    /// <summary>
    /// Publishes a vote response decision.
    /// </summary>
    /// <param name="decision">Vote response decision.</param>
    public void Publish(VoteResponseDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);

        LogEvents(decision.Events);
        PublishStatus(decision.StatusSnapshot);
    }

    /// <summary>
    /// Publishes an append-entries response decision.
    /// </summary>
    /// <param name="decision">Append-entries response decision.</param>
    public void Publish(AppendEntriesResponseDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);

        LogEvents(decision.Events);
    }

    /// <summary>
    /// Publishes a single raft event.
    /// </summary>
    /// <param name="raftEvent">Raft event.</param>
    public void Publish(RaftEvent raftEvent)
    {
        ArgumentNullException.ThrowIfNull(raftEvent);

        _eventLog.WriteNodeEvent(_settings.NodeId, raftEvent);
    }

    private void PublishStatus(RaftStatus? status)
    {
        if (status is not null)
        {
            _log.WriteNodeStatus(status);
        }
    }

    private void LogEvents(IEnumerable<RaftEvent> events)
    {
        foreach (var raftEvent in events)
        {
            Publish(raftEvent);
        }
    }

    private readonly RaftSettings _settings;
    private readonly IRaftLog _log;
    private readonly IRaftEventLog _eventLog;
}
