using FluentAssertions;

using RaftSimulator.Models.Domain;

namespace RaftSimulator.Tests.Models.Domain;

public sealed class RaftNodeStateTests
{
    [Fact(DisplayName = "StartElection advances term and votes for self")]
    [Trait("Category", "Unit")]
    public void StartElectionAdvancesTermAndVotesForSelf()
    {
        // Arrange
        var state = new RaftNodeState();
        var now = TestNow;
        state.InitializeFollower(now, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));

        // Act
        state.StartElection(1, now, TimeSpan.FromSeconds(5));

        // Assert
        state.Role.Should().Be(RaftRole.Candidate);
        state.CurrentTerm.Should().Be(new Term(1));
        state.VotedFor.Should().Be(new CandidateId(1));
        state.VotesReceived.Should().Be(1);
        state.NextElectionDeadline.Should().Be(now + TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "BecomeFollower clears leader tracking and updates newer term")]
    [Trait("Category", "Unit")]
    public void BecomeFollowerClearsLeaderTrackingAndUpdatesNewerTerm()
    {
        // Arrange
        var state = new RaftNodeState();
        var now = TestNow;
        state.InitializeFollower(now, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));
        state.StartElection(1, now, TimeSpan.FromSeconds(4));
        state.RecordGrantedVote();
        state.BecomeLeader(1, 2, now);
        state.RegisterHeartbeatAck(2, now);

        // Act
        state.BecomeFollower(2, 3, now, TimeSpan.FromSeconds(5));

        // Assert
        state.Role.Should().Be(RaftRole.Follower);
        state.CurrentTerm.Should().Be(new Term(2));
        state.LeaderId.Should().Be(new LeaderId(3));
        state.VotedFor.Should().BeNull();
        state.LeaderSince.Should().Be(default);
        state.LastHeartbeatAckAt.Should().BeEmpty();
    }

    [Fact(DisplayName = "BecomeLeader records local leader state")]
    [Trait("Category", "Unit")]
    public void BecomeLeaderRecordsLocalLeaderState()
    {
        // Arrange
        var state = new RaftNodeState();
        var now = TestNow;
        state.InitializeFollower(now, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));
        state.StartElection(1, now, TimeSpan.FromSeconds(4));
        state.RecordGrantedVote();

        // Act
        state.BecomeLeader(1, 2, now);

        // Assert
        state.Role.Should().Be(RaftRole.Leader);
        state.LeaderId.Should().Be(new LeaderId(1));
        state.VotesReceived.Should().Be(0);
        state.NextHeartbeatAt.Should().Be(now);
        state.LeaderSince.Should().Be(now);
    }

    [Fact(DisplayName = "TryGrantVote records vote only when request is grantable")]
    [Trait("Category", "Unit")]
    public void TryGrantVoteRecordsVoteOnlyWhenRequestIsGrantable()
    {
        // Arrange
        var state = new RaftNodeState();
        var now = TestNow;
        state.InitializeFollower(now, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));
        var firstRequest = new RaftVoteRequest(new Term(0), new CandidateId(2));
        var secondRequest = new RaftVoteRequest(new Term(0), new CandidateId(3));

        // Act
        var firstGranted = state.TryGrantVote(firstRequest, now, TimeSpan.FromSeconds(5));
        var secondGranted = state.TryGrantVote(secondRequest, now, TimeSpan.FromSeconds(6));

        // Assert
        firstGranted.Should().BeTrue();
        secondGranted.Should().BeFalse();
        state.VotedFor.Should().Be(new CandidateId(2));
        state.NextElectionDeadline.Should().Be(now + TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "AcceptHeartbeat records leader only for current follower term")]
    [Trait("Category", "Unit")]
    public void AcceptHeartbeatRecordsLeaderOnlyForCurrentFollowerTerm()
    {
        // Arrange
        var state = new RaftNodeState();
        var now = TestNow;
        state.InitializeFollower(now, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));
        var request = new RaftAppendEntriesRequest(new Term(0), new LeaderId(2));

        // Act
        state.AcceptHeartbeat(request, now, TimeSpan.FromSeconds(5));

        // Assert
        state.LeaderId.Should().Be(new LeaderId(2));
        state.NextElectionDeadline.Should().Be(now + TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "StartElection rejects leader transition")]
    [Trait("Category", "Unit")]
    public void StartElectionRejectsLeaderTransition()
    {
        // Arrange
        var state = new RaftNodeState();
        var now = TestNow;
        state.InitializeFollower(now, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));
        state.StartElection(1, now, TimeSpan.FromSeconds(4));
        state.RecordGrantedVote();
        state.BecomeLeader(1, 2, now);

        // Act
        var act = () => state.StartElection(1, now, TimeSpan.FromSeconds(4));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Leader cannot start a new election.");
    }

    [Fact(DisplayName = "BecomeLeader rejects non-candidate and candidate without majority")]
    [Trait("Category", "Unit")]
    public void BecomeLeaderRejectsInvalidTransitions()
    {
        // Arrange
        var state = new RaftNodeState();
        var now = TestNow;
        state.InitializeFollower(now, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));

        // Act
        var followerAct = () => state.BecomeLeader(1, 2, now);

        // Assert
        followerAct.Should().Throw<InvalidOperationException>()
            .WithMessage("Only candidate can become leader.");

        state.StartElection(1, now, TimeSpan.FromSeconds(4));
        var candidateWithoutMajorityAct = () => state.BecomeLeader(1, 2, now);

        candidateWithoutMajorityAct.Should().Throw<InvalidOperationException>()
            .WithMessage("Candidate cannot become leader before receiving majority.");
    }

    [Fact(DisplayName = "BecomeFollower rejects older term")]
    [Trait("Category", "Unit")]
    public void BecomeFollowerRejectsOlderTerm()
    {
        // Arrange
        var state = new RaftNodeState();
        var now = TestNow;
        state.InitializeFollower(now, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));
        state.StartElection(1, now, TimeSpan.FromSeconds(4));

        // Act
        var act = () => state.BecomeFollower(0, null, now, TimeSpan.FromSeconds(5));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Node cannot move to an older term.");
    }

    [Fact(DisplayName = "Heartbeat-only operations reject non-leader state")]
    [Trait("Category", "Unit")]
    public void HeartbeatOnlyOperationsRejectNonLeaderState()
    {
        // Arrange
        var state = new RaftNodeState();
        var now = TestNow;
        state.InitializeFollower(now, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));

        // Act
        var scheduleAct = () => state.ScheduleHeartbeat(now, TimeSpan.FromSeconds(1));
        var ackAct = () => state.RegisterHeartbeatAck(2, now);

        // Assert
        scheduleAct.Should().Throw<InvalidOperationException>()
            .WithMessage("Only leader can schedule heartbeats.");
        ackAct.Should().Throw<InvalidOperationException>()
            .WithMessage("Only leader can register heartbeat acknowledgements.");
    }

    [Fact(DisplayName = "Stale vote and heartbeat requests are rejected")]
    [Trait("Category", "Unit")]
    public void StaleVoteAndHeartbeatRequestsAreRejected()
    {
        // Arrange
        var state = new RaftNodeState();
        var now = TestNow;
        state.InitializeFollower(now, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1));
        state.StartElection(1, now, TimeSpan.FromSeconds(4));
        state.BecomeFollower(1, null, now, TimeSpan.FromSeconds(4));
        var staleVote = new RaftVoteRequest(new Term(0), new CandidateId(2));
        var staleHeartbeat = new RaftAppendEntriesRequest(new Term(0), new LeaderId(2));

        // Act
        var voteAct = () => state.TryGrantVote(staleVote, now, TimeSpan.FromSeconds(5));
        var heartbeatAct = () => state.AcceptHeartbeat(staleHeartbeat, now, TimeSpan.FromSeconds(5));

        // Assert
        voteAct.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot grant a stale vote request.");
        heartbeatAct.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot accept a stale heartbeat.");
    }

    [Fact(DisplayName = "InitializeFollower rejects non-positive timeouts")]
    [Trait("Category", "Unit")]
    public void InitializeFollowerRejectsNonPositiveTimeouts()
    {
        // Arrange
        var state = new RaftNodeState();
        var now = TestNow;

        // Act
        var electionAct = () => state.InitializeFollower(now, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        var heartbeatAct = () => state.InitializeFollower(now, TimeSpan.FromSeconds(1), TimeSpan.Zero);

        // Assert
        electionAct.Should().Throw<ArgumentOutOfRangeException>();
        heartbeatAct.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "StartElection rejects invalid arguments")]
    [Trait("Category", "Unit")]
    public void StartElectionRejectsInvalidArguments()
    {
        // Arrange
        var state = new RaftNodeState();
        var now = TestNow;

        // Act
        var nodeAct = () => state.StartElection(0, now, TimeSpan.FromSeconds(1));
        var timeoutAct = () => state.StartElection(1, now, TimeSpan.Zero);

        // Assert
        nodeAct.Should().Throw<ArgumentOutOfRangeException>();
        timeoutAct.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "BecomeLeader rejects invalid arguments")]
    [Trait("Category", "Unit")]
    public void BecomeLeaderRejectsInvalidArguments()
    {
        // Arrange
        var state = new RaftNodeState();
        var now = TestNow;

        // Act
        var nodeAct = () => state.BecomeLeader(0, 1, now);
        var majorityAct = () => state.BecomeLeader(1, 0, now);

        // Assert
        nodeAct.Should().Throw<ArgumentOutOfRangeException>();
        majorityAct.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "BecomeFollower rejects invalid arguments")]
    [Trait("Category", "Unit")]
    public void BecomeFollowerRejectsInvalidArguments()
    {
        // Arrange
        var state = new RaftNodeState();
        var now = TestNow;

        // Act
        var termAct = () => state.BecomeFollower(-1, null, now, TimeSpan.FromSeconds(1));
        var leaderAct = () => state.BecomeFollower(1, 0, now, TimeSpan.FromSeconds(1));
        var timeoutAct = () => state.BecomeFollower(1, null, now, TimeSpan.Zero);

        // Assert
        termAct.Should().Throw<ArgumentOutOfRangeException>();
        leaderAct.Should().Throw<ArgumentOutOfRangeException>();
        timeoutAct.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "TryGrantVote rejects non-positive election timeout")]
    [Trait("Category", "Unit")]
    public void TryGrantVoteRejectsNonPositiveElectionTimeout()
    {
        // Arrange
        var state = new RaftNodeState();
        var now = TestNow;

        // Act
        var act = () => state.TryGrantVote(new RaftVoteRequest(1, 1), now, TimeSpan.Zero);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "TryGrantVote rejects null request")]
    [Trait("Category", "Unit")]
    public void TryGrantVoteRejectsNullRequest()
    {
        // Arrange
        var state = new RaftNodeState();

        // Act
        var act = () => state.TryGrantVote(null!, TestNow, TimeSpan.FromSeconds(1));

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "AcceptHeartbeat rejects non-positive election timeout")]
    [Trait("Category", "Unit")]
    public void AcceptHeartbeatRejectsNonPositiveElectionTimeout()
    {
        // Arrange
        var state = new RaftNodeState();
        var now = TestNow;

        // Act
        var act = () => state.AcceptHeartbeat(new RaftAppendEntriesRequest(1, 1), now, TimeSpan.Zero);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "AcceptHeartbeat rejects null request")]
    [Trait("Category", "Unit")]
    public void AcceptHeartbeatRejectsNullRequest()
    {
        // Arrange
        var state = new RaftNodeState();

        // Act
        var act = () => state.AcceptHeartbeat(null!, TestNow, TimeSpan.FromSeconds(1));

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "HasMajority rejects invalid majority")]
    [Trait("Category", "Unit")]
    public void HasMajorityRejectsInvalidMajority()
    {
        // Arrange
        var state = new RaftNodeState();

        // Act
        var act = () => state.HasMajority(0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "ScheduleElection rejects non-positive timeout")]
    [Trait("Category", "Unit")]
    public void ScheduleElectionRejectsNonPositiveTimeout()
    {
        // Arrange
        var state = new RaftNodeState();

        // Act
        var act = () => state.ScheduleElection(TestNow, TimeSpan.Zero);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "ScheduleHeartbeat rejects non-positive interval")]
    [Trait("Category", "Unit")]
    public void ScheduleHeartbeatRejectsNonPositiveInterval()
    {
        // Arrange
        var state = new RaftNodeState();

        // Act
        var act = () => state.ScheduleHeartbeat(TestNow, TimeSpan.Zero);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "RegisterHeartbeatAck rejects invalid peer id")]
    [Trait("Category", "Unit")]
    public void RegisterHeartbeatAckRejectsInvalidPeerId()
    {
        // Arrange
        var state = new RaftNodeState();

        // Act
        var act = () => state.RegisterHeartbeatAck(0, TestNow);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static readonly DateTimeOffset TestNow = new(2026, 5, 17, 12, 0, 0, TimeSpan.Zero);
}
