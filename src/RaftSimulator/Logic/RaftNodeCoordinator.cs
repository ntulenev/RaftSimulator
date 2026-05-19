using RaftSimulator.Abstractions;
using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;
using RaftSimulator.Models.Domain.Events;

namespace RaftSimulator.Logic;

/// <summary>
/// Coordinates locked access to raft node state transitions.
/// </summary>
internal sealed class RaftNodeCoordinator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RaftNodeCoordinator"/> class.
    /// </summary>
    /// <param name="settings">Raft settings.</param>
    /// <param name="clock">Clock.</param>
    /// <param name="delayProvider">Delay provider.</param>
    /// <param name="runtime">Runtime loop.</param>
    public RaftNodeCoordinator(
        RaftSettings settings,
        IRaftClock clock,
        IRaftDelayProvider delayProvider,
        IRaftNodeRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(delayProvider);
        ArgumentNullException.ThrowIfNull(runtime);

        _settings = settings;
        _clock = clock;
        _delayProvider = delayProvider;
        _runtime = runtime;
        _stateMachine = new RaftStateMachine(settings);
    }

    /// <summary>
    /// Initializes state as a follower.
    /// </summary>
    public void InitializeState()
    {
        WithStateLock(
            () => _stateMachine.Initialize(
                _clock.UtcNow,
                GetRandomElectionTimeout(),
                _settings.HeartbeatInterval),
            signalScheduleChange: true);
    }

    /// <summary>
    /// Handles a request-vote RPC.
    /// </summary>
    /// <param name="request">Vote request.</param>
    /// <returns>Vote decision.</returns>
    public VoteDecision HandleRequestVote(RaftVoteRequest request)
    {
        return WithStateLock(
            () => _stateMachine.HandleRequestVote(
                request,
                _clock.UtcNow,
                GetRandomElectionTimeout()),
            signalScheduleChange: true);
    }

    /// <summary>
    /// Handles an append-entries RPC.
    /// </summary>
    /// <param name="request">Append entries request.</param>
    /// <returns>Append entries decision.</returns>
    public AppendEntriesDecision HandleAppendEntries(RaftAppendEntriesRequest request)
    {
        return WithStateLock(
            () => _stateMachine.HandleAppendEntries(
                request,
                _clock.UtcNow,
                GetRandomElectionTimeout()),
            signalScheduleChange: true);
    }

    /// <summary>
    /// Prepares work for an elapsed timeout.
    /// </summary>
    /// <returns>Timeout action.</returns>
    public TimeoutAction PrepareTimeoutAction()
    {
        return WithStateLock(
            () => _stateMachine.PrepareTimeoutAction(
                _clock.UtcNow,
                GetRandomElectionTimeout(),
                _settings.HeartbeatInterval),
            signalScheduleChange: true);
    }

    /// <summary>
    /// Handles a vote response from a peer.
    /// </summary>
    /// <param name="response">Vote response.</param>
    /// <returns>Vote response decision.</returns>
    public VoteResponseDecision HandleVoteResponse(RaftVoteResponse response)
    {
        return WithStateLock(
            () => _stateMachine.HandleVoteResponse(
                response,
                _clock.UtcNow,
                GetRandomElectionTimeout()),
            static decision => decision.Events.Count > 0 || decision.BecameLeader);
    }

    /// <summary>
    /// Handles an append-entries response from a peer.
    /// </summary>
    /// <param name="response">Append entries response.</param>
    /// <returns>Append entries response decision.</returns>
    public AppendEntriesResponseDecision HandleAppendEntriesResponse(RaftAppendEntriesResponse response)
    {
        return WithStateLock(
            () => _stateMachine.HandleAppendEntriesResponse(
                response,
                _clock.UtcNow,
                GetRandomElectionTimeout()),
            static decision => decision.Events.Count > 0);
    }

    /// <summary>
    /// Registers a successful heartbeat acknowledgement.
    /// </summary>
    /// <param name="peerId">Peer identifier.</param>
    public void RegisterHeartbeatAck(FromId peerId)
    {
        WithStateLock(
            () => _stateMachine.RegisterHeartbeatAck(peerId, _clock.UtcNow),
            signalScheduleChange: false);
    }

    /// <summary>
    /// Builds an out-of-quorum event when the current leader cannot reach majority.
    /// </summary>
    /// <param name="window">Quorum freshness window.</param>
    /// <returns>Out-of-quorum event, or null.</returns>
    public RaftEvent? BuildQuorumEvent(TimeSpan window)
    {
        return WithStateLock(
            () => _stateMachine.BuildQuorumEvent(_clock.UtcNow, window),
            signalScheduleChange: false);
    }

    /// <summary>
    /// Gets delay until the next scheduled state-machine action.
    /// </summary>
    /// <returns>Delay until next action.</returns>
    public TimeSpan GetNextDelay()
    {
        return WithStateLock(
            () => _stateMachine.GetNextDelay(_clock.UtcNow),
            signalScheduleChange: false);
    }

    /// <summary>
    /// Gets current node status.
    /// </summary>
    /// <returns>Status snapshot.</returns>
    public RaftStatus GetStatus()
    {
        return WithStateLock(
            _stateMachine.GetStatus,
            signalScheduleChange: false);
    }

    private TimeSpan GetRandomElectionTimeout() =>
        _delayProvider.GetDelay(_settings.MinElectionTimeout, _settings.MaxElectionTimeout);

    private void WithStateLock(Action action, bool signalScheduleChange)
    {
        lock (_gate)
        {
            action();
            if (signalScheduleChange)
            {
                SignalScheduleChangeUnsafe();
            }
        }
    }

    private T WithStateLock<T>(Func<T> action, bool signalScheduleChange)
    {
        lock (_gate)
        {
            var result = action();
            if (signalScheduleChange)
            {
                SignalScheduleChangeUnsafe();
            }

            return result;
        }
    }

    private T WithStateLock<T>(Func<T> action, Func<T, bool> shouldSignalScheduleChange)
    {
        lock (_gate)
        {
            var result = action();
            if (shouldSignalScheduleChange(result))
            {
                SignalScheduleChangeUnsafe();
            }

            return result;
        }
    }

    private void SignalScheduleChangeUnsafe() =>
        _runtime.Signal();

    private readonly RaftSettings _settings;
    private readonly IRaftClock _clock;
    private readonly IRaftDelayProvider _delayProvider;
    private readonly IRaftNodeRuntime _runtime;
    private readonly RaftStateMachine _stateMachine;
    private readonly Lock _gate = new();
}
