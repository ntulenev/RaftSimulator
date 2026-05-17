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
        state.BecomeLeader(1, now);
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

        // Act
        state.BecomeLeader(1, now);

        // Assert
        state.Role.Should().Be(RaftRole.Leader);
        state.LeaderId.Should().Be(new LeaderId(1));
        state.VotesReceived.Should().Be(0);
        state.NextHeartbeatAt.Should().Be(now);
        state.LeaderSince.Should().Be(now);
    }

    private static readonly DateTimeOffset TestNow = new(2026, 5, 17, 12, 0, 0, TimeSpan.Zero);
}
