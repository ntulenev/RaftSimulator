using FluentAssertions;

using RaftSimulator.Models.Domain;
using RaftSimulator.Models.Domain.Events;

namespace RaftSimulator.Tests.Models.Domain;

public sealed class RaftDomainEventsTests
{
    [Fact(DisplayName = "Domain events expose validated values")]
    [Trait("Category", "Unit")]
    public void DomainEventsExposeValidatedValues()
    {
        // Act
        RaftEvent[] events =
        [
            new BecameFollowerEvent(new Term(2), new LeaderId(1)),
            new BecameLeaderEvent(new Term(2)),
            new ElectionTimeoutEvent(new Term(2)),
            new HeartbeatIgnoredEvent(new LeaderId(1), new Term(2)),
            new HeartbeatReceivedEvent(new LeaderId(1), new Term(2)),
            new HigherTermDiscoveredEvent(new Term(2), new FromId(1)),
            new LeaderHeartbeatEvent(),
            new OutOfQuorumEvent(1, 3, 2),
            new RequestVoteDeniedEvent(new CandidateId(1), new Term(2)),
            new RequestVoteGrantedEvent(new CandidateId(1), new Term(2)),
            new VoteResponseDeniedEvent(new FromId(1), new Term(2)),
            new VoteResponseGrantedEvent(new FromId(1), 1, 2)
        ];

        // Assert
        events.Should().HaveCount(12);
        events[0].Should().Be(new BecameFollowerEvent(new Term(2), new LeaderId(1)));
        events[1].Should().Be(new BecameLeaderEvent(new Term(2)));
        events[2].Should().Be(new ElectionTimeoutEvent(new Term(2)));
        events[3].Should().Be(new HeartbeatIgnoredEvent(new LeaderId(1), new Term(2)));
        events[4].Should().Be(new HeartbeatReceivedEvent(new LeaderId(1), new Term(2)));
        events[5].Should().Be(new HigherTermDiscoveredEvent(new Term(2), new FromId(1)));
        events[6].Should().Be(new LeaderHeartbeatEvent());
        events[7].Should().Be(new OutOfQuorumEvent(1, 3, 2));
        events[8].Should().Be(new RequestVoteDeniedEvent(new CandidateId(1), new Term(2)));
        events[9].Should().Be(new RequestVoteGrantedEvent(new CandidateId(1), new Term(2)));
        events[10].Should().Be(new VoteResponseDeniedEvent(new FromId(1), new Term(2)));
        events[11].Should().Be(new VoteResponseGrantedEvent(new FromId(1), 1, 2));
    }

    [Fact(DisplayName = "Domain events allow unknown follower leader")]
    [Trait("Category", "Unit")]
    public void DomainEventsAllowUnknownFollowerLeader()
    {
        // Act
        var raftEvent = new BecameFollowerEvent(new Term(2), null);

        // Assert
        raftEvent.Term.Should().Be(new Term(2));
        raftEvent.LeaderId.Should().BeNull();
    }

    [Fact(DisplayName = "Term domain events reject negative terms")]
    [Trait("Category", "Unit")]
    public void TermDomainEventsRejectNegativeTerms()
    {
        // Act
        Action[] acts =
        [
            () => _ = new BecameFollowerEvent(new Term(-1), null),
            () => _ = new BecameLeaderEvent(new Term(-1)),
            () => _ = new ElectionTimeoutEvent(new Term(-1)),
            () => _ = new HeartbeatIgnoredEvent(new LeaderId(1), new Term(-1)),
            () => _ = new HeartbeatReceivedEvent(new LeaderId(1), new Term(-1)),
            () => _ = new HigherTermDiscoveredEvent(new Term(-1), new FromId(1)),
            () => _ = new RequestVoteDeniedEvent(new CandidateId(1), new Term(-1)),
            () => _ = new RequestVoteGrantedEvent(new CandidateId(1), new Term(-1)),
            () => _ = new VoteResponseDeniedEvent(new FromId(1), new Term(-1))
        ];

        // Assert
        foreach (var act in acts)
        {
            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }

    [Fact(DisplayName = "Node domain events reject non-positive ids")]
    [Trait("Category", "Unit")]
    public void NodeDomainEventsRejectNonPositiveIds()
    {
        // Act
        Action[] acts =
        [
            () => _ = new BecameFollowerEvent(new Term(1), new LeaderId(0)),
            () => _ = new HeartbeatIgnoredEvent(new LeaderId(0), new Term(1)),
            () => _ = new HeartbeatReceivedEvent(new LeaderId(0), new Term(1)),
            () => _ = new HigherTermDiscoveredEvent(new Term(1), new FromId(0)),
            () => _ = new RequestVoteDeniedEvent(new CandidateId(0), new Term(1)),
            () => _ = new RequestVoteGrantedEvent(new CandidateId(0), new Term(1)),
            () => _ = new VoteResponseDeniedEvent(new FromId(0), new Term(1)),
            () => _ = new VoteResponseGrantedEvent(new FromId(0), 1, 2)
        ];

        // Assert
        foreach (var act in acts)
        {
            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }

    [Fact(DisplayName = "Vote granted event rejects invalid vote counts")]
    [Trait("Category", "Unit")]
    public void VoteGrantedEventRejectsInvalidVoteCounts()
    {
        // Act
        Action[] acts =
        [
            () => _ = new VoteResponseGrantedEvent(new FromId(1), 0, 2),
            () => _ = new VoteResponseGrantedEvent(new FromId(1), 1, 0),
            () => _ = new VoteResponseGrantedEvent(new FromId(1), 3, 2)
        ];

        // Assert
        foreach (var act in acts)
        {
            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }

    [Fact(DisplayName = "Out of quorum event rejects impossible counts")]
    [Trait("Category", "Unit")]
    public void OutOfQuorumEventRejectsImpossibleCounts()
    {
        // Act
        Action[] acts =
        [
            () => _ = new OutOfQuorumEvent(0, 3, 2),
            () => _ = new OutOfQuorumEvent(1, 0, 2),
            () => _ = new OutOfQuorumEvent(1, 3, 0),
            () => _ = new OutOfQuorumEvent(4, 3, 2),
            () => _ = new OutOfQuorumEvent(1, 3, 4),
            () => _ = new OutOfQuorumEvent(2, 3, 2)
        ];

        // Assert
        foreach (var act in acts)
        {
            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
