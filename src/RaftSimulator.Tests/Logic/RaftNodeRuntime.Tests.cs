using FluentAssertions;

using RaftSimulator.Abstractions;
using RaftSimulator.Logic;
using RaftSimulator.Logic.Events;
using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;
using RaftSimulator.Transport;

namespace RaftSimulator.Tests.Logic;

public sealed class RaftNodeRuntimeTests
{
    [Fact(DisplayName = "RunAsync starts election when scheduler timeout elapses")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncStartsElectionWhenSchedulerTimeoutElapses()
    {
        // Arrange
        var settings = CreateSettings();
        var clock = new TestClock();
        var scheduler = new TimeoutThenCancelScheduler(() => clock.Advance(TimeSpan.FromSeconds(5)));
        var broadcaster = new SpyBroadcaster();
        var log = new TestRaftLog();
        var eventLog = new TestRaftEventLog();
        var node = new RaftNode(
            settings,
            broadcaster,
            log,
            eventLog,
            clock,
            new FixedRandom(),
            scheduler);

        // Act
        await node.RunAsync(CancellationToken.None);

        // Assert
        broadcaster.RequestVoteCalls.Should().Be(1);
        broadcaster.LastVoteTerm.Should().Be(1);
        broadcaster.LastCandidateId.Should().Be(1);
        eventLog.Events.Should().ContainSingle(item => item.Event is ElectionTimeoutEvent);
        log.Messages.Should().Contain("Started as follower.");
        log.Messages.Should().Contain("Stopped.");
    }

    [Fact(DisplayName = "RunAsync sends heartbeats after winning an election")]
    [Trait("Category", "Unit")]
    public async Task RunAsyncSendsHeartbeatsAfterWinningAnElection()
    {
        // Arrange
        var settings = CreateSettings();
        var clock = new TestClock();
        var scheduler = new TimeoutThenCancelScheduler(() => clock.Advance(TimeSpan.FromSeconds(5)));
        var broadcaster = new SpyBroadcaster
        {
            VoteResults =
            [
                PeerRpcResult<RaftVoteResponse>.Success(
                    settings.Peers[0],
                    new RaftVoteResponse(1, settings.Peers[0].Id, true))
            ]
        };
        var node = new RaftNode(
            settings,
            broadcaster,
            new TestRaftLog(),
            new TestRaftEventLog(),
            clock,
            new FixedRandom(),
            scheduler);

        // Act
        await node.RunAsync(CancellationToken.None);

        // Assert
        broadcaster.SendHeartbeatCalls.Should().Be(1);
        broadcaster.LastHeartbeatTerm.Should().Be(1);
        broadcaster.LastLeaderId.Should().Be(1);
    }

    private static RaftSettings CreateSettings()
    {
        var options = new RaftOptions
        {
            NodeId = 1,
            Port = 5001,
            Peers = "1=http://localhost:5001;2=http://localhost:5002;3=http://localhost:5003",
            HeartbeatSeconds = 1,
            MinElectionSeconds = 4,
            MaxElectionSeconds = 4,
            MinNetworkDelaySeconds = 0,
            MaxNetworkDelaySeconds = 0
        };

        return RaftSettings.FromOptions(options);
    }

    private sealed class TimeoutThenCancelScheduler(Action beforeTimeout) : IRaftScheduler
    {
        private int _waitCalls;

        public int SignalCalls { get; private set; }

        public void Signal() => SignalCalls++;

        public Task<RaftScheduleWaitResult> WaitAsync(
            Func<TimeSpan> getNextDelay,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(getNextDelay);
            cancellationToken.ThrowIfCancellationRequested();

            _waitCalls++;
            if (_waitCalls == 1)
            {
                _ = getNextDelay();
                beforeTimeout();
                return Task.FromResult(RaftScheduleWaitResult.Timeout);
            }

            throw new OperationCanceledException(cancellationToken);
        }
    }

    private sealed class TestClock : IRaftClock
    {
        public DateTimeOffset UtcNow { get; private set; } =
            new(2026, 5, 16, 12, 0, 0, TimeSpan.Zero);

        public void Advance(TimeSpan value) =>
            UtcNow += value;
    }

    private sealed class FixedRandom : IRaftRandom
    {
        public double NextDouble() => 0;
    }

    private sealed class SpyBroadcaster : IRaftPeerBroadcaster
    {
        public IReadOnlyList<PeerRpcResult<RaftVoteResponse>> VoteResults { get; init; } = [];

        public int RequestVoteCalls { get; private set; }

        public int SendHeartbeatCalls { get; private set; }

        public int LastVoteTerm { get; private set; }

        public int LastCandidateId { get; private set; }

        public int LastHeartbeatTerm { get; private set; }

        public int LastLeaderId { get; private set; }

        public Task<IReadOnlyList<PeerRpcResult<RaftVoteResponse>>> RequestVotesAsync(
            int term,
            int candidateId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RequestVoteCalls++;
            LastVoteTerm = term;
            LastCandidateId = candidateId;
            return Task.FromResult(VoteResults);
        }

        public Task<IReadOnlyList<PeerRpcResult<RaftAppendEntriesResponse>>> SendHeartbeatsAsync(
            int term,
            int leaderId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SendHeartbeatCalls++;
            LastHeartbeatTerm = term;
            LastLeaderId = leaderId;
            return Task.FromResult<IReadOnlyList<PeerRpcResult<RaftAppendEntriesResponse>>>([]);
        }
    }

    private sealed class TestRaftLog : IRaftLog
    {
        public List<string> Messages { get; } = [];

        public void WriteNode(int nodeId, string message) =>
            Messages.Add(message);

        public void WriteSystem(string message) =>
            Messages.Add(message);

        public void WriteNodeStatus(RaftStatus status)
        {
        }
    }

    private sealed class TestRaftEventLog : IRaftEventLog
    {
        public List<(int NodeId, RaftEvent Event)> Events { get; } = [];

        public void WriteNodeEvent(int nodeId, RaftEvent raftEvent) =>
            Events.Add((nodeId, raftEvent));
    }
}
