using FluentAssertions;

using RaftSimulator.Abstractions;
using RaftSimulator.Logic;
using RaftSimulator.Logic.Events;
using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;

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
        var electionRunner = new SpyElectionRunner();
        var log = new TestRaftLog();
        var eventLog = new TestRaftEventLog();
        var node = new RaftNode(
            settings,
            log,
            eventLog,
            clock,
            new FixedDelayProvider(),
            scheduler,
            electionRunner,
            new SpyHeartbeatRunner());

        // Act
        await node.RunAsync(CancellationToken.None);

        // Assert
        electionRunner.StartElectionCalls.Should().Be(1);
        electionRunner.LastVoteTerm.Should().Be(1);
        electionRunner.LastCandidateId.Should().Be(1);
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
        var electionRunner = new SpyElectionRunner
        {
            VoteResults =
            [
                new RaftVoteResponse(1, settings.Peers[0].Id, true)
            ]
        };
        var heartbeatRunner = new SpyHeartbeatRunner();
        var node = new RaftNode(
            settings,
            new TestRaftLog(),
            new TestRaftEventLog(),
            clock,
            new FixedDelayProvider(),
            scheduler,
            electionRunner,
            heartbeatRunner);

        // Act
        await node.RunAsync(CancellationToken.None);

        // Assert
        heartbeatRunner.SendHeartbeatCalls.Should().Be(1);
        heartbeatRunner.LastHeartbeatTerm.Should().Be(1);
        heartbeatRunner.LastLeaderId.Should().Be(1);
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

    private sealed class FixedDelayProvider : IRaftDelayProvider
    {
        public TimeSpan GetDelay(TimeSpan min, TimeSpan max) => min;
    }

    private sealed class SpyElectionRunner : IRaftElectionRunner
    {
        public IReadOnlyList<RaftVoteResponse> VoteResults { get; init; } = [];

        public int StartElectionCalls { get; private set; }

        public int LastVoteTerm { get; private set; }

        public int LastCandidateId { get; private set; }

        public async Task StartElectionAsync(
            int term,
            int candidateId,
            Func<RaftVoteResponse, CancellationToken, Task> handleVoteResponseAsync,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(handleVoteResponseAsync);
            cancellationToken.ThrowIfCancellationRequested();

            StartElectionCalls++;
            LastVoteTerm = term;
            LastCandidateId = candidateId;

            foreach (var response in VoteResults)
            {
                await handleVoteResponseAsync(response, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private sealed class SpyHeartbeatRunner : IRaftHeartbeatRunner
    {
        public int SendHeartbeatCalls { get; private set; }

        public int LastHeartbeatTerm { get; private set; }

        public int LastLeaderId { get; private set; }

        public Task SendHeartbeatsAsync(
            int term,
            int leaderId,
            Action reportQuorum,
            Action<RaftAppendEntriesResponse> handleAppendEntriesResponse,
            Action<int> registerHeartbeatAck,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(reportQuorum);
            ArgumentNullException.ThrowIfNull(handleAppendEntriesResponse);
            ArgumentNullException.ThrowIfNull(registerHeartbeatAck);
            cancellationToken.ThrowIfCancellationRequested();

            SendHeartbeatCalls++;
            LastHeartbeatTerm = term;
            LastLeaderId = leaderId;
            reportQuorum();
            return Task.CompletedTask;
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
