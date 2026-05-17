using FluentAssertions;

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

    private static readonly string[] ExpectedRoles = ["Follower", "Candidate", "Leader"];
}
