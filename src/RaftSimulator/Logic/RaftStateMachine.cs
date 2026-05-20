using RaftSimulator.Models.Domain.Events;
using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;

namespace RaftSimulator.Logic;

/// <summary>
/// Pure raft election state transitions for a single node.
/// </summary>
internal sealed class RaftStateMachine
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RaftStateMachine"/> class.
    /// </summary>
    /// <param name="settings">Raft settings.</param>
    public RaftStateMachine(RaftSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _context = new RaftStateMachineContext(settings);
        _requestVoteHandler = new RaftRequestVoteHandler(_context);
        _appendEntriesHandler = new RaftAppendEntriesHandler(_context);
        _timeoutHandler = new RaftTimeoutHandler(_context);
        _voteResponseHandler = new RaftVoteResponseHandler(_context);
        _appendEntriesResponseHandler = new RaftAppendEntriesResponseHandler(_context);
    }

    /// <summary>
    /// Gets local node identifier.
    /// </summary>
    public int Id => _context.Id;

    /// <summary>
    /// Initializes state as a follower.
    /// </summary>
    /// <param name="now">Current time.</param>
    /// <param name="electionTimeout">Initial election timeout.</param>
    /// <param name="heartbeatInterval">Heartbeat interval.</param>
    public void Initialize(
        DateTimeOffset now,
        TimeSpan electionTimeout,
        TimeSpan heartbeatInterval)
    {
        _context.State.InitializeFollower(now, electionTimeout, heartbeatInterval);
        _context.StatusReporter.Reset();
    }

    /// <summary>
    /// Handles a request-vote RPC.
    /// </summary>
    /// <param name="request">Vote request.</param>
    /// <param name="now">Current time.</param>
    /// <param name="electionTimeout">Next election timeout.</param>
    /// <returns>Vote decision.</returns>
    public VoteDecision HandleRequestVote(
        RaftVoteRequest request,
        DateTimeOffset now,
        TimeSpan electionTimeout) =>
        _requestVoteHandler.Handle(request, now, electionTimeout);

    /// <summary>
    /// Handles an append-entries RPC.
    /// </summary>
    /// <param name="request">Append entries request.</param>
    /// <param name="now">Current time.</param>
    /// <param name="electionTimeout">Next election timeout.</param>
    /// <returns>Append entries decision.</returns>
    public AppendEntriesDecision HandleAppendEntries(
        RaftAppendEntriesRequest request,
        DateTimeOffset now,
        TimeSpan electionTimeout) =>
        _appendEntriesHandler.Handle(request, now, electionTimeout);

    /// <summary>
    /// Prepares work for the next elapsed timeout.
    /// </summary>
    /// <param name="now">Current time.</param>
    /// <param name="electionTimeout">Next election timeout.</param>
    /// <param name="heartbeatInterval">Heartbeat interval.</param>
    /// <returns>Timeout action.</returns>
    public TimeoutAction PrepareTimeoutAction(
        DateTimeOffset now,
        TimeSpan electionTimeout,
        TimeSpan heartbeatInterval) =>
        _timeoutHandler.Prepare(now, electionTimeout, heartbeatInterval);

    /// <summary>
    /// Handles a vote response from a peer.
    /// </summary>
    /// <param name="response">Vote response.</param>
    /// <param name="now">Current time.</param>
    /// <param name="electionTimeout">Next election timeout.</param>
    /// <returns>Vote response decision.</returns>
    public VoteResponseDecision HandleVoteResponse(
        RaftVoteResponse response,
        DateTimeOffset now,
        TimeSpan electionTimeout) =>
        _voteResponseHandler.Handle(response, now, electionTimeout);

    /// <summary>
    /// Handles an append-entries response from a peer.
    /// </summary>
    /// <param name="response">Append entries response.</param>
    /// <param name="now">Current time.</param>
    /// <param name="electionTimeout">Next election timeout.</param>
    /// <returns>Append entries response decision.</returns>
    public AppendEntriesResponseDecision HandleAppendEntriesResponse(
        RaftAppendEntriesResponse response,
        DateTimeOffset now,
        TimeSpan electionTimeout) =>
        _appendEntriesResponseHandler.Handle(response, now, electionTimeout);

    /// <summary>
    /// Registers a successful heartbeat acknowledgement.
    /// </summary>
    /// <param name="peerId">Peer node identifier.</param>
    /// <param name="now">Current time.</param>
    public void RegisterHeartbeatAck(FromId peerId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(peerId);

        _context.State.RegisterHeartbeatAck(peerId.Value, now);
    }

    /// <summary>
    /// Builds an out-of-quorum event when the current leader cannot reach majority.
    /// </summary>
    /// <param name="now">Current time.</param>
    /// <param name="window">Quorum freshness window.</param>
    /// <returns>Out-of-quorum event, or null when quorum is available or not yet reportable.</returns>
    public RaftEvent? BuildQuorumEvent(DateTimeOffset now, TimeSpan window)
    {
        if (_context.State.LeaderSince == default || now - _context.State.LeaderSince < window)
        {
            return null;
        }

        return RaftQuorumEvaluator.BuildOutOfQuorumEvent(
            _context.Settings.Peers,
            _context.State.GetLastHeartbeatAckSnapshot(),
            _context.Settings.Majority,
            _context.Settings.NodeCount,
            now,
            window);
    }

    /// <summary>
    /// Gets the delay until the next scheduled state-machine action.
    /// </summary>
    /// <param name="now">Current time.</param>
    /// <returns>Delay until the next action.</returns>
    public TimeSpan GetNextDelay(DateTimeOffset now)
    {
        var deadline = _context.State.Role == RaftRole.Leader
            ? _context.State.NextHeartbeatAt
            : _context.State.NextElectionDeadline;
        return deadline - now;
    }

    /// <summary>
    /// Gets a snapshot of current node status.
    /// </summary>
    /// <returns>Status snapshot.</returns>
    public RaftStatus GetStatus() =>
        _context.GetStatus();

    private readonly RaftStateMachineContext _context;
    private readonly RaftRequestVoteHandler _requestVoteHandler;
    private readonly RaftAppendEntriesHandler _appendEntriesHandler;
    private readonly RaftTimeoutHandler _timeoutHandler;
    private readonly RaftVoteResponseHandler _voteResponseHandler;
    private readonly RaftAppendEntriesResponseHandler _appendEntriesResponseHandler;
}
