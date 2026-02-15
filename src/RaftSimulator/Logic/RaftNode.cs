using System.Security.Cryptography;
using System.Threading.Channels;

using RaftSimulator.Abstractions;
using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;

namespace RaftSimulator.Logic;

/// <summary>
/// Raft node state machine.
/// </summary>
internal sealed class RaftNode : IRaftNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RaftNode"/> class.
    /// </summary>
    /// <param name="settings">Raft settings.</param>
    /// <param name="peerClient">Peer client.</param>
    /// <param name="log">Log sink.</param>
    public RaftNode(RaftSettings settings, IRaftPeerClient peerClient, IRaftLog log)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(peerClient);
        ArgumentNullException.ThrowIfNull(log);

        _settings = settings;
        _peerClient = peerClient;
        _log = log;
        _scheduleSignal = Channel.CreateUnbounded<bool>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
    }

    /// <inheritdoc />
    public int Id => _settings.NodeId;

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        InitializeState();
        _log.WriteNode(Id, "Started as follower.");

        try
        {
            while (true)
            {
                var delay = GetNextDelay();
                if (delay < TimeSpan.Zero)
                {
                    delay = TimeSpan.Zero;
                }

                var delayTask = Task.Delay(delay, cancellationToken);
                var signalTask = _scheduleSignal.Reader.ReadAsync(cancellationToken).AsTask();
                var completed = await Task
                    .WhenAny(delayTask, signalTask)
                    .ConfigureAwait(false);

                if (completed == signalTask)
                {
                    DrainScheduleSignals();
                    continue;
                }

                await HandleTimeoutAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _log.WriteNode(Id, "Stopped.");
        }
    }

    /// <inheritdoc />
    public Task<RaftVoteResponse> OnRequestVoteAsync(
        RaftVoteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        RaftVoteResponse response;
        string logLine;

        lock (_gate)
        {
            if (request.Term < _currentTerm)
            {
                response = new RaftVoteResponse(_currentTerm, Id, false);
                logLine = $"Denied vote to Node {request.CandidateId:00} (term {request.Term}).";
            }
            else
            {
                logLine = request.Term > _currentTerm
                    ? BecomeFollowerUnsafe(request.Term, null)
                    : string.Empty;

                var canVote = _role != RaftRole.Leader
                    && (_votedFor is null || _votedFor == request.CandidateId);
                if (canVote)
                {
                    _votedFor = request.CandidateId;
                    _nextElectionDeadline = GetNextElectionDeadlineUnsafe();
                    SignalScheduleChangeUnsafe();
                }

                response = new RaftVoteResponse(_currentTerm, Id, canVote);
                logLine = string.IsNullOrWhiteSpace(logLine)
                    ? $"{(canVote ? "Granted" : "Denied")} vote to Node {request.CandidateId:00} " +
                      $"(term {_currentTerm})."
                    : logLine + $" {(canVote ? "Granted" : "Denied")} vote to Node " +
                      $"{request.CandidateId:00} (term {_currentTerm}).";
            }
        }

        _log.WriteNode(Id, logLine);
        return Task.FromResult(response);
    }

    /// <inheritdoc />
    public Task<RaftAppendEntriesResponse> OnAppendEntriesAsync(
        RaftAppendEntriesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        RaftAppendEntriesResponse response;
        string logLine;
        RaftStatus? statusSnapshot = null;

        lock (_gate)
        {
            if (request.Term < _currentTerm)
            {
                response = new RaftAppendEntriesResponse(_currentTerm, Id, false);
                logLine = $"Ignored heartbeat from Node {request.LeaderId:00} (term {request.Term}).";
            }
            else
            {
                logLine = request.Term > _currentTerm || _role != RaftRole.Follower
                    ? BecomeFollowerUnsafe(request.Term, request.LeaderId)
                    : string.Empty;

                _leaderId = request.LeaderId;
                _nextElectionDeadline = GetNextElectionDeadlineUnsafe();
                SignalScheduleChangeUnsafe();

                response = new RaftAppendEntriesResponse(_currentTerm, Id, true);
                logLine = string.IsNullOrWhiteSpace(logLine)
                    ? $"Heartbeat from Node {request.LeaderId:00} (term {_currentTerm})."
                    : logLine + $" Heartbeat from Node {request.LeaderId:00} (term {_currentTerm}).";

                if (TryGetElectionStatusSnapshotUnsafe(out var snapshot))
                {
                    statusSnapshot = snapshot;
                }
            }
        }

        _log.WriteNode(Id, logLine);
        if (statusSnapshot is not null)
        {
            _log.WriteNodeStatus(statusSnapshot);
        }
        return Task.FromResult(response);
    }

    /// <inheritdoc />
    public RaftStatus GetStatus()
    {
        lock (_gate)
        {
            return new RaftStatus(
                Id,
                _currentTerm,
                _role,
                _leaderId);
        }
    }

    private void InitializeState()
    {
        lock (_gate)
        {
            _role = RaftRole.Follower;
            _currentTerm = 0;
            _votedFor = null;
            _leaderId = null;
            _votesReceived = 0;
            _lastReportedTerm = -1;
            _nextElectionDeadline = GetNextElectionDeadlineUnsafe();
            _nextHeartbeatAt = DateTimeOffset.UtcNow + _settings.HeartbeatInterval;
        }
    }

    private async Task HandleTimeoutAsync(CancellationToken cancellationToken)
    {
        if (GetNextDelay() > TimeSpan.Zero)
        {
            return;
        }

        var action = PrepareTimeoutAction();
        _log.WriteNode(Id, action.LogLine);

        if (action.Type == TimeoutActionType.Heartbeats)
        {
            await SendHeartbeatsAsync(action.Term, cancellationToken).ConfigureAwait(false);
            return;
        }

        await StartElectionAsync(action.Term, cancellationToken).ConfigureAwait(false);
    }

    private TimeoutAction PrepareTimeoutAction()
    {
        lock (_gate)
        {
            if (_role == RaftRole.Leader)
            {
                _nextHeartbeatAt = DateTimeOffset.UtcNow + _settings.HeartbeatInterval;
                SignalScheduleChangeUnsafe();
                return new TimeoutAction(
                    TimeoutActionType.Heartbeats,
                    _currentTerm,
                    "Leader heartbeat.");
            }

            _role = RaftRole.Candidate;
            _currentTerm++;
            _votedFor = Id;
            _votesReceived = 1;
            _leaderId = null;
            _nextElectionDeadline = GetNextElectionDeadlineUnsafe();
            SignalScheduleChangeUnsafe();

            return new TimeoutAction(
                TimeoutActionType.Election,
                _currentTerm,
                $"Election timeout. Term {_currentTerm}, becoming candidate.");
        }
    }

    private Task StartElectionAsync(int term, CancellationToken cancellationToken)
    {
        foreach (var peer in _settings.Peers)
        {
            _ = RequestVoteFromPeerAsync(peer, term, cancellationToken);
        }

        return Task.CompletedTask;
    }

    private async Task RequestVoteFromPeerAsync(
        PeerInfo peer,
        int term,
        CancellationToken cancellationToken)
    {
        try
        {
            await DelayNetworkAsync(cancellationToken).ConfigureAwait(false);

            _log.WriteNode(Id, $"RequestVote -> Node {peer.Id:00} (term {term}).");

            var response = await _peerClient
                .RequestVoteAsync(peer, new RaftVoteRequest(term, Id), cancellationToken)
                .ConfigureAwait(false);

            if (response is null)
            {
                _log.WriteNode(Id, $"VoteResponse unavailable from Node {peer.Id:00}.");
                return;
            }

            await HandleVoteResponseAsync(response, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (HttpRequestException)
        {
            _log.WriteNode(Id, $"Unable to reach Node {peer.Id:00}.");
        }
        catch (TaskCanceledException)
        {
            _log.WriteNode(Id, $"Unable to reach Node {peer.Id:00}.");
        }
    }

    private async Task HandleVoteResponseAsync(
        RaftVoteResponse response,
        CancellationToken cancellationToken)
    {
        var logs = new List<string>(2);
        var becameLeader = false;
        var term = response.Term;
        RaftStatus? statusSnapshot = null;

        lock (_gate)
        {
            if (response.Term > _currentTerm)
            {
                logs.Add(
                    $"Discovered higher term {response.Term} from Node {response.FromId:00}.");
                logs.Add(BecomeFollowerUnsafe(response.Term, null));
            }
            else if (_role != RaftRole.Candidate || response.Term != _currentTerm)
            {
                return;
            }
            else if (!response.Granted)
            {
                logs.Add(
                    $"Vote denied by Node {response.FromId:00} (term {_currentTerm}).");
            }
            else
            {
                _votesReceived++;
                logs.Add(
                    $"Vote granted by Node {response.FromId:00}. Total={_votesReceived}/{_settings.Majority}.");

                if (_votesReceived >= _settings.Majority)
                {
                    _role = RaftRole.Leader;
                    _leaderId = Id;
                    _votesReceived = 0;
                    _nextHeartbeatAt = DateTimeOffset.UtcNow;
                    SignalScheduleChangeUnsafe();
                    logs.Add($"Became leader for term {_currentTerm}.");
                    becameLeader = true;
                    term = _currentTerm;

                    if (TryGetElectionStatusSnapshotUnsafe(out var snapshot))
                    {
                        statusSnapshot = snapshot;
                    }
                }
            }
        }

        foreach (var logLine in logs)
        {
            _log.WriteNode(Id, logLine);
        }

        if (statusSnapshot is not null)
        {
            _log.WriteNodeStatus(statusSnapshot);
        }

        if (becameLeader)
        {
            await SendHeartbeatsAsync(term, cancellationToken).ConfigureAwait(false);
        }
    }

    private Task SendHeartbeatsAsync(int term, CancellationToken cancellationToken)
    {
        var roundId = StartHeartbeatRound();
        foreach (var peer in _settings.Peers)
        {
            _ = SendHeartbeatToPeerAsync(peer, term, roundId, cancellationToken);
        }
        return Task.CompletedTask;
    }

    private async Task SendHeartbeatToPeerAsync(
        PeerInfo peer,
        int term,
        int roundId,
        CancellationToken cancellationToken)
    {
        try
        {
            await DelayNetworkAsync(cancellationToken).ConfigureAwait(false);

            _log.WriteNode(Id, $"AppendEntries -> Node {peer.Id:00} (term {term}).");

            var response = await _peerClient
                .AppendEntriesAsync(peer, new RaftAppendEntriesRequest(term, Id), cancellationToken)
                .ConfigureAwait(false);

            if (response is null)
            {
                _log.WriteNode(Id, $"AppendEntries unavailable from Node {peer.Id:00}.");
                return;
            }

            RegisterHeartbeatAck(roundId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (HttpRequestException)
        {
            _log.WriteNode(Id, $"Unable to reach Node {peer.Id:00}.");
        }
        catch (TaskCanceledException)
        {
            _log.WriteNode(Id, $"Unable to reach Node {peer.Id:00}.");
        }
    }

    private string BecomeFollowerUnsafe(int term, int? leaderId)
    {
        _role = RaftRole.Follower;
        _leaderId = leaderId;
        _votesReceived = 0;

        if (term > _currentTerm)
        {
            _currentTerm = term;
            _votedFor = null;
        }

        _nextElectionDeadline = GetNextElectionDeadlineUnsafe();
        SignalScheduleChangeUnsafe();

        var leaderText = leaderId is null ? "unknown" : $"Node {leaderId:00}";
        return $"Became follower for term {_currentTerm} (leader {leaderText}).";
    }

    private bool TryGetElectionStatusSnapshotUnsafe(out RaftStatus snapshot)
    {
        if (_leaderId is null || _currentTerm <= _lastReportedTerm)
        {
            snapshot = default!;
            return false;
        }

        _lastReportedTerm = _currentTerm;
        snapshot = new RaftStatus(Id, _currentTerm, _role, _leaderId);
        return true;
    }

    private TimeSpan GetNextDelay()
    {
        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;
            var deadline = _role == RaftRole.Leader
                ? _nextHeartbeatAt
                : _nextElectionDeadline;
            return deadline - now;
        }
    }

    private DateTimeOffset GetNextElectionDeadlineUnsafe() =>
        DateTimeOffset.UtcNow + GetRandomElectionTimeoutUnsafe();

    private TimeSpan GetRandomElectionTimeoutUnsafe()
    {
        if (_settings.MinElectionTimeout == _settings.MaxElectionTimeout)
        {
            return _settings.MinElectionTimeout;
        }

        var window = _settings.MaxElectionTimeout - _settings.MinElectionTimeout;
        var offset = TimeSpan.FromMilliseconds(
            window.TotalMilliseconds * NextRandomDouble());
        return _settings.MinElectionTimeout + offset;
    }

    private Task DelayNetworkAsync(CancellationToken cancellationToken)
    {
        var delay = GetRandomNetworkDelay();
        return delay > TimeSpan.Zero
            ? Task.Delay(delay, cancellationToken)
            : Task.CompletedTask;
    }

    private TimeSpan GetRandomNetworkDelay()
    {
        if (_settings.MinNetworkDelay == _settings.MaxNetworkDelay)
        {
            return _settings.MinNetworkDelay;
        }

        var window = _settings.MaxNetworkDelay - _settings.MinNetworkDelay;
        var offset = TimeSpan.FromMilliseconds(
            window.TotalMilliseconds * NextRandomDouble());
        return _settings.MinNetworkDelay + offset;
    }

    private static double NextRandomDouble()
    {
        var value = RandomNumberGenerator.GetInt32(int.MaxValue);
        return value / (double)int.MaxValue;
    }

    private void DrainScheduleSignals()
    {
        while (_scheduleSignal.Reader.TryRead(out _))
        {
        }
    }

    private void SignalScheduleChangeUnsafe() =>
        _ = _scheduleSignal.Writer.TryWrite(true);

    private int StartHeartbeatRound()
    {
        var previousRoundId = Volatile.Read(ref _heartbeatRoundId);
        var previousAcks = Volatile.Read(ref _heartbeatAcks);

        if (previousRoundId > 0)
        {
            ReportQuorum(previousAcks);
        }

        var newRoundId = Interlocked.Increment(ref _heartbeatRoundId);
        _ = Interlocked.Exchange(ref _heartbeatAcks, 0);
        return newRoundId;
    }

    private void RegisterHeartbeatAck(int roundId)
    {
        if (roundId != Volatile.Read(ref _heartbeatRoundId))
        {
            return;
        }

        _ = Interlocked.Increment(ref _heartbeatAcks);
    }

    private void ReportQuorum(int peerAcks)
    {
        var reachable = peerAcks + 1;
        var needed = _settings.Majority;
        var total = _settings.NodeCount;

        if (reachable >= needed)
        {
            return;
        }

        _log.WriteNode(Id, $"Cluster out of quorum: {reachable}/{total} (need {needed}).");
    }

    private readonly RaftSettings _settings;
    private readonly IRaftPeerClient _peerClient;
    private readonly IRaftLog _log;
    private readonly Channel<bool> _scheduleSignal;
    private readonly Lock _gate = new();

    private RaftRole _role;
    private int _currentTerm;
    private int? _votedFor;
    private int? _leaderId;
    private int _votesReceived;
    private DateTimeOffset _nextElectionDeadline;
    private DateTimeOffset _nextHeartbeatAt;
    private int _lastReportedTerm;
    private int _heartbeatRoundId;
    private int _heartbeatAcks;

    private enum TimeoutActionType
    {
        Election,
        Heartbeats
    }

    private sealed record TimeoutAction(TimeoutActionType Type, int Term, string LogLine);
}
