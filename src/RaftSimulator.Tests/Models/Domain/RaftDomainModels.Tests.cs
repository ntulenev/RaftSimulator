using FluentAssertions;

using RaftSimulator.Logic.Events;
using RaftSimulator.Models.Domain;

namespace RaftSimulator.Tests.Models.Domain;

public sealed class RaftDomainModelsTests
{
    [Fact(DisplayName = "Vote models expose values and equality")]
    [Trait("Category", "Unit")]
    public void VoteModelsExposeValuesAndEquality()
    {
        // Act
        var request = new RaftVoteRequest(3, 2);
        var response = new RaftVoteResponse(3, 2, true);

        // Assert
        request.Term.Should().Be(new Term(3));
        request.CandidateId.Should().Be(new CandidateId(2));
        request.Should().Be(new RaftVoteRequest(3, 2));
        response.FromId.Should().Be(new FromId(2));
        response.Granted.Should().BeTrue();
    }

    [Fact(DisplayName = "Append entries models expose values")]
    [Trait("Category", "Unit")]
    public void AppendEntriesModelsExposeValues()
    {
        // Act
        var request = new RaftAppendEntriesRequest(4, 1);
        var response = new RaftAppendEntriesResponse(4, 2, true);

        // Assert
        request.Term.Should().Be(new Term(4));
        request.LeaderId.Should().Be(new LeaderId(1));
        response.FromId.Should().Be(new FromId(2));
        response.Success.Should().BeTrue();
    }

    [Fact(DisplayName = "Status model exposes values")]
    [Trait("Category", "Unit")]
    public void StatusModelExposesValues()
    {
        // Act
        var status = new RaftStatus(1, 5, RaftRole.Leader, 1);

        // Assert
        status.NodeId.Should().Be(new NodeId(1));
        status.Term.Should().Be(new Term(5));
        status.Role.Should().Be(RaftRole.Leader);
        status.LeaderId.Should().Be(new LeaderId(1));
        status.HasKnownLeader.Should().BeTrue();
    }

    [Fact(DisplayName = "RaftRole defines expected values")]
    [Trait("Category", "Unit")]
    public void RaftRoleDefinesExpectedValues()
    {
        // Act
        var names = Enum.GetNames<RaftRole>();

        // Assert
        names.Should().Contain(ExpectedRoles);
    }

    [Fact(DisplayName = "Identifier value objects format invariant values")]
    [Trait("Category", "Unit")]
    public void IdentifierValueObjectsFormatInvariantValues()
    {
        // Act
        var candidate = new CandidateId(2);
        var from = new FromId(3);
        var leader = new LeaderId(4);
        var node = new NodeId(5);
        var term = new Term(6);

        // Assert
        candidate.ToString().Should().Be("2");
        candidate.ToString("00", null).Should().Be("02");
        from.ToString().Should().Be("3");
        from.ToString("00", null).Should().Be("03");
        leader.ToString().Should().Be("4");
        leader.ToString("00", null).Should().Be("04");
        node.ToString().Should().Be("5");
        node.ToString("00", null).Should().Be("05");
        term.ToString().Should().Be("6");
        term.ToString("00", null).Should().Be("06");
    }

    [Theory(DisplayName = "Node identifier value objects reject non-positive values")]
    [Trait("Category", "Unit")]
    [InlineData(0)]
    [InlineData(-1)]
    public void NodeIdentifierValueObjectsRejectNonPositiveValues(int value)
    {
        // Act
        Action[] acts =
        [
            () => _ = new NodeId(value),
            () => _ = new CandidateId(value),
            () => _ = new LeaderId(value),
            () => _ = new FromId(value)
        ];

        // Assert
        foreach (var act in acts)
        {
            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }

    [Fact(DisplayName = "Term rejects negative values and advances itself")]
    [Trait("Category", "Unit")]
    public void TermRejectsNegativeValuesAndAdvancesItself()
    {
        // Act
        var next = Term.Initial.Next();
        var negative = () => new Term(-1);

        // Assert
        next.Should().Be(new Term(1));
        next.IsNewerThan(Term.Initial).Should().BeTrue();
        Term.Initial.IsOlderThan(next).Should().BeTrue();
        negative.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Vote request evaluates grant rules")]
    [Trait("Category", "Unit")]
    public void VoteRequestEvaluatesGrantRules()
    {
        // Arrange
        var request = new RaftVoteRequest(2, 3);

        // Assert
        request.IsStaleFor(new Term(3)).Should().BeTrue();
        request.AdvancesTerm(new Term(1)).Should().BeTrue();
        request.CanBeGrantedBy(RaftRole.Follower, null).Should().BeTrue();
        request.CanBeGrantedBy(RaftRole.Follower, new CandidateId(3)).Should().BeTrue();
        request.CanBeGrantedBy(RaftRole.Follower, new CandidateId(2)).Should().BeFalse();
        request.CanBeGrantedBy(RaftRole.Leader, null).Should().BeFalse();
    }

    [Fact(DisplayName = "Vote response evaluates term relation")]
    [Trait("Category", "Unit")]
    public void VoteResponseEvaluatesTermRelation()
    {
        // Arrange
        var response = new RaftVoteResponse(2, 3, true);

        // Assert
        response.HasHigherTermThan(new Term(1)).Should().BeTrue();
        response.IsForTerm(new Term(2)).Should().BeTrue();
        response.IsForTerm(new Term(3)).Should().BeFalse();
    }

    [Fact(DisplayName = "Append entries request evaluates follower transition rules")]
    [Trait("Category", "Unit")]
    public void AppendEntriesRequestEvaluatesFollowerTransitionRules()
    {
        // Arrange
        var request = new RaftAppendEntriesRequest(2, 1);

        // Assert
        request.IsStaleFor(new Term(3)).Should().BeTrue();
        request.ShouldMakeFollower(new Term(1), RaftRole.Follower).Should().BeTrue();
        request.ShouldMakeFollower(new Term(2), RaftRole.Leader).Should().BeTrue();
        request.ShouldMakeFollower(new Term(2), RaftRole.Follower).Should().BeFalse();
    }

    [Fact(DisplayName = "Append entries response evaluates term relation")]
    [Trait("Category", "Unit")]
    public void AppendEntriesResponseEvaluatesTermRelation()
    {
        // Arrange
        var response = new RaftAppendEntriesResponse(2, 3, true);

        // Assert
        response.HasHigherTermThan(new Term(1)).Should().BeTrue();
        response.HasHigherTermThan(new Term(2)).Should().BeFalse();
    }

    [Fact(DisplayName = "Raft RPC models reject null value objects")]
    [Trait("Category", "Unit")]
    public void RaftRpcModelsRejectNullValueObjects()
    {
        // Act
        Action[] acts =
        [
            () => _ = new RaftVoteRequest(null!, 1),
            () => _ = new RaftVoteRequest(1, null!),
            () => _ = new RaftVoteResponse(null!, 1, true),
            () => _ = new RaftVoteResponse(1, null!, true),
            () => _ = new RaftAppendEntriesRequest(null!, 1),
            () => _ = new RaftAppendEntriesRequest(1, null!),
            () => _ = new RaftAppendEntriesResponse(null!, 1, true),
            () => _ = new RaftAppendEntriesResponse(1, null!, true),
            () => _ = new RaftStatus(null!, 1, RaftRole.Follower, null),
            () => _ = new RaftStatus(1, null!, RaftRole.Follower, null)
        ];

        // Assert
        foreach (var act in acts)
        {
            act.Should().Throw<ArgumentNullException>();
        }
    }

    [Fact(DisplayName = "Domain decisions reject invalid constructor arguments")]
    [Trait("Category", "Unit")]
    public void DomainDecisionsRejectInvalidConstructorArguments()
    {
        // Arrange
        var response = new RaftVoteResponse(1, 2, true);
        var appendResponse = new RaftAppendEntriesResponse(1, 2, true);
        var raftEvent = new LeaderHeartbeatEvent();

        // Act
        Action[] nullActs =
        [
            () => _ = new VoteDecision(null!, []),
            () => _ = new VoteDecision(response, null!),
            () => _ = new AppendEntriesDecision(null!, [], null),
            () => _ = new AppendEntriesDecision(appendResponse, null!, null),
            () => _ = new AppendEntriesResponseDecision(null!),
            () => _ = new HeartbeatRunResult(null!, []),
            () => _ = new HeartbeatRunResult([], null!),
            () => _ = new TimeoutAction(TimeoutActionType.Election, 1, null!),
            () => _ = new VoteResponseDecision(null!, false, 1, null)
        ];

        Action[] invalidActs =
        [
            () => _ = new VoteDecision(response, [null!]),
            () => _ = new AppendEntriesDecision(appendResponse, [null!], null),
            () => _ = new AppendEntriesResponseDecision([null!]),
            () => _ = new HeartbeatRunResult([null!], []),
            () => _ = new HeartbeatRunResult([], [0]),
            () => _ = new TimeoutAction((TimeoutActionType)99, 1, [raftEvent]),
            () => _ = new TimeoutAction(TimeoutActionType.Election, -1, [raftEvent]),
            () => _ = new TimeoutAction(TimeoutActionType.Election, 1, [null!]),
            () => _ = new VoteResponseDecision([null!], false, 1, null),
            () => _ = new VoteResponseDecision([], false, -1, null)
        ];

        // Assert
        foreach (var act in nullActs)
        {
            act.Should().Throw<ArgumentNullException>();
        }

        foreach (var act in invalidActs)
        {
            act.Should().Throw<ArgumentException>();
        }
    }

    [Fact(DisplayName = "Domain methods reject invalid arguments")]
    [Trait("Category", "Unit")]
    public void DomainMethodsRejectInvalidArguments()
    {
        // Arrange
        var voteRequest = new RaftVoteRequest(1, 2);
        var appendRequest = new RaftAppendEntriesRequest(1, 2);
        var voteResponse = new RaftVoteResponse(1, 2, true);
        var appendResponse = new RaftAppendEntriesResponse(1, 2, true);

        // Act
        Action[] nullActs =
        [
            () => _ = voteRequest.IsStaleFor(null!),
            () => _ = voteRequest.AdvancesTerm(null!),
            () => _ = appendRequest.IsStaleFor(null!),
            () => _ = appendRequest.ShouldMakeFollower(null!, RaftRole.Follower),
            () => _ = voteResponse.HasHigherTermThan(null!),
            () => _ = voteResponse.IsForTerm(null!),
            () => _ = appendResponse.HasHigherTermThan(null!)
        ];

        Action[] invalidArgumentActs =
        [
            () => _ = voteRequest.CanBeGrantedBy((RaftRole)99, null),
            () => _ = appendRequest.ShouldMakeFollower(1, (RaftRole)99),
            () => _ = new RaftStatus(1, 1, (RaftRole)99, null)
        ];

        var invalidOperationAct = () => new Term(int.MaxValue).Next();

        // Assert
        foreach (var act in nullActs)
        {
            act.Should().Throw<ArgumentNullException>();
        }

        foreach (var act in invalidArgumentActs)
        {
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        invalidOperationAct.Should().Throw<InvalidOperationException>();
    }

    private static readonly string[] ExpectedRoles = ["Follower", "Candidate", "Leader"];
}
