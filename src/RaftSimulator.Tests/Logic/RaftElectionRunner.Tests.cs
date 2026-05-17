using FluentAssertions;

using RaftSimulator.Abstractions;
using RaftSimulator.Logic;
using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;

namespace RaftSimulator.Tests.Logic;

public sealed class RaftElectionRunnerTests
{
    [Fact(DisplayName = "StartElection invokes handler for successful vote responses")]
    [Trait("Category", "Unit")]
    public async Task StartElectionInvokesHandlerForSuccessfulVoteResponses()
    {
        // Arrange
        var peer = CreatePeer(2);
        var response = new RaftVoteResponse(3, 2, true);
        var broadcaster = new TestBroadcaster
        {
            VoteResults = [PeerRpcResult<RaftVoteResponse>.Success(peer, response)]
        };
        var log = new TestRaftLog();
        var runner = new RaftElectionRunner(broadcaster, log);
        var handled = new List<RaftVoteResponse>();

        // Act
        await runner.StartElectionAsync(
            3,
            1,
            (voteResponse, _) =>
            {
                handled.Add(voteResponse);
                return Task.CompletedTask;
            },
            CancellationToken.None);

        // Assert
        broadcaster.RequestVoteCalls.Should().Be(1);
        broadcaster.LastVoteTerm.Should().Be(3);
        broadcaster.LastCandidateId.Should().Be(1);
        handled.Should().ContainSingle().Which.Should().Be(response);
        log.Messages.Should().Contain("RequestVote -> Node 02 (term 3).");
    }

    [Fact(DisplayName = "StartElection logs unavailable vote responses without invoking handler")]
    [Trait("Category", "Unit")]
    public async Task StartElectionLogsUnavailableVoteResponsesWithoutInvokingHandler()
    {
        // Arrange
        var peer = CreatePeer(2);
        var broadcaster = new TestBroadcaster
        {
            VoteResults = [PeerRpcResult<RaftVoteResponse>.Unavailable(peer)]
        };
        var log = new TestRaftLog();
        var runner = new RaftElectionRunner(broadcaster, log);
        var handledResponses = 0;

        // Act
        await runner.StartElectionAsync(
            3,
            1,
            (_, _) =>
            {
                handledResponses++;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        // Assert
        handledResponses.Should().Be(0);
        log.Messages.Should().Contain("VoteResponse unavailable from Node 02.");
    }

    [Fact(DisplayName = "StartElection logs peer failures")]
    [Trait("Category", "Unit")]
    public async Task StartElectionLogsPeerFailures()
    {
        // Arrange
        var peer = CreatePeer(2);
        var broadcaster = new TestBroadcaster
        {
            VoteResults =
            [
                PeerRpcResult<RaftVoteResponse>.Failed(peer, new InvalidOperationException("boom"))
            ]
        };
        var log = new TestRaftLog();
        var runner = new RaftElectionRunner(broadcaster, log);

        // Act
        await runner.StartElectionAsync(
            3,
            1,
            (_, _) => Task.CompletedTask,
            CancellationToken.None);

        // Assert
        log.Messages.Should().Contain(
            "RequestVote (term 3) -> Node 02 failed: InvalidOperationException: boom");
    }

    [Fact(DisplayName = "StartElection logs transport failures as unreachable")]
    [Trait("Category", "Unit")]
    public async Task StartElectionLogsTransportFailuresAsUnreachable()
    {
        // Arrange
        var peer = CreatePeer(2);
        var broadcaster = new TestBroadcaster
        {
            VoteResults =
            [
                PeerRpcResult<RaftVoteResponse>.Failed(peer, new HttpRequestException("offline"))
            ]
        };
        var log = new TestRaftLog();
        var runner = new RaftElectionRunner(broadcaster, log);

        // Act
        await runner.StartElectionAsync(
            3,
            1,
            (_, _) => Task.CompletedTask,
            CancellationToken.None);

        // Assert
        log.Messages.Should().Contain("Unable to reach Node 02.");
    }

    private static PeerInfo CreatePeer(int id) =>
        new(id, new Uri($"http://localhost:500{id}"));

    private sealed class TestBroadcaster : IRaftPeerBroadcaster
    {
        public IReadOnlyList<PeerRpcResult<RaftVoteResponse>> VoteResults { get; init; } = [];

        public int RequestVoteCalls { get; private set; }

        public int LastVoteTerm { get; private set; }

        public int LastCandidateId { get; private set; }

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
            throw new NotSupportedException();
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
}
