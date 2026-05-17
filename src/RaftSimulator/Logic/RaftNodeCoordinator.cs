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
        lock (_gate)
        {
            _stateMachine.Initialize(
                _clock.UtcNow,
                GetRandomElectionTimeout(),
                _settings.HeartbeatInterval);
            SignalScheduleChangeUnsafe();
        }
    }

    /// <summary>
    /// Handles a request-vote RPC.
    /// </summary>
    /// <param name="request">Vote request.</param>
    /// <returns>Vote decision.</returns>
    public VoteDecision HandleRequestVote(RaftVoteRequest request)
    {
        lock (_gate)
        {
            var decision = _stateMachine.HandleRequestVote(
                request,
                _clock.UtcNow,
                GetRandomElectionTimeout());
            SignalScheduleChangeUnsafe();
            return decision;
        }
    }

    /// <summary>
    /// Handles an append-entries RPC.
    /// </summary>
    /// <param name="request">Append entries request.</param>
    /// <returns>Append entries decision.</returns>
    public AppendEntriesDecision HandleAppendEntries(RaftAppendEntriesRequest request)
    {
        lock (_gate)
        {
            var decision = _stateMachine.HandleAppendEntries(
                request,
                _clock.UtcNow,
                GetRandomElectionTimeout());
            SignalScheduleChangeUnsafe();
            return decision;
        }
    }

    /// <summary>
    /// Prepares work for an elapsed timeout.
    /// </summary>
    /// <returns>Timeout action.</returns>
    public TimeoutAction PrepareTimeoutAction()
    {
        lock (_gate)
        {
            var action = _stateMachine.PrepareTimeoutAction(
                _clock.UtcNow,
                GetRandomElectionTimeout(),
                _settings.HeartbeatInterval);
            SignalScheduleChangeUnsafe();
            return action;
        }
    }

    /// <summary>
    /// Handles a vote response from a peer.
    /// </summary>
    /// <param name="response">Vote response.</param>
    /// <returns>Vote response decision.</returns>
    public VoteResponseDecision HandleVoteResponse(RaftVoteResponse response)
    {
        lock (_gate)
        {
            var decision = _stateMachine.HandleVoteResponse(
                response,
                _clock.UtcNow,
                GetRandomElectionTimeout());
            if (decision.Events.Count > 0 || decision.BecameLeader)
            {
                SignalScheduleChangeUnsafe();
            }

            return decision;
        }
    }

    /// <summary>
    /// Handles an append-entries response from a peer.
    /// </summary>
    /// <param name="response">Append entries response.</param>
    /// <returns>Append entries response decision.</returns>
    public AppendEntriesResponseDecision HandleAppendEntriesResponse(RaftAppendEntriesResponse response)
    {
        lock (_gate)
        {
            var decision = _stateMachine.HandleAppendEntriesResponse(
                response,
                _clock.UtcNow,
                GetRandomElectionTimeout());
            if (decision.Events.Count > 0)
            {
                SignalScheduleChangeUnsafe();
            }

            return decision;
        }
    }

    /// <summary>
    /// Registers a successful heartbeat acknowledgement.
    /// </summary>
    /// <param name="peerId">Peer identifier.</param>
    public void RegisterHeartbeatAck(FromId peerId)
    {
        lock (_gate)
        {
            _stateMachine.RegisterHeartbeatAck(peerId, _clock.UtcNow);
        }
    }

    /// <summary>
    /// Builds an out-of-quorum event when the current leader cannot reach majority.
    /// </summary>
    /// <param name="window">Quorum freshness window.</param>
    /// <returns>Out-of-quorum event, or null.</returns>
    public RaftEvent? BuildQuorumEvent(TimeSpan window)
    {
        lock (_gate)
        {
            return _stateMachine.BuildQuorumEvent(_clock.UtcNow, window);
        }
    }

    /// <summary>
    /// Gets delay until the next scheduled state-machine action.
    /// </summary>
    /// <returns>Delay until next action.</returns>
    public TimeSpan GetNextDelay()
    {
        lock (_gate)
        {
            return _stateMachine.GetNextDelay(_clock.UtcNow);
        }
    }

    /// <summary>
    /// Gets current node status.
    /// </summary>
    /// <returns>Status snapshot.</returns>
    public RaftStatus GetStatus()
    {
        lock (_gate)
        {
            return _stateMachine.GetStatus();
        }
    }

    private TimeSpan GetRandomElectionTimeout() =>
        _delayProvider.GetDelay(_settings.MinElectionTimeout, _settings.MaxElectionTimeout);

    private void SignalScheduleChangeUnsafe() =>
        _runtime.Signal();

    private readonly RaftSettings _settings;
    private readonly IRaftClock _clock;
    private readonly IRaftDelayProvider _delayProvider;
    private readonly IRaftNodeRuntime _runtime;
    private readonly RaftStateMachine _stateMachine;
    private readonly Lock _gate = new();
}
