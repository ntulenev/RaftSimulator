using FluentAssertions;

using RaftSimulator.Contracts;

namespace RaftSimulator.Tests.Contracts;

public sealed class RaftDtosTests
{
    [Fact(DisplayName = "Vote DTOs expose values")]
    [Trait("Category", "Unit")]
    public void VoteDtosExposeValues()
    {
        // Act
        var request = new RaftVoteRequestDto(2, 3);
        var response = new RaftVoteResponseDto(2, 4, true);

        // Assert
        request.Term.Should().Be(2);
        request.CandidateId.Should().Be(3);
        response.FromId.Should().Be(4);
        response.Granted.Should().BeTrue();
    }

    [Fact(DisplayName = "Append entries DTOs expose values")]
    [Trait("Category", "Unit")]
    public void AppendEntriesDtosExposeValues()
    {
        // Act
        var request = new RaftAppendEntriesRequestDto(1, 2);
        var response = new RaftAppendEntriesResponseDto(1, 3, true);

        // Assert
        request.Term.Should().Be(1);
        request.LeaderId.Should().Be(2);
        response.FromId.Should().Be(3);
        response.Success.Should().BeTrue();
    }

    [Fact(DisplayName = "Status DTO exposes values")]
    [Trait("Category", "Unit")]
    public void StatusDtoExposesValues()
    {
        // Act
        var status = new RaftStatusDto(1, 4, "Leader", 1);

        // Assert
        status.NodeId.Should().Be(1);
        status.Term.Should().Be(4);
        status.Role.Should().Be("Leader");
        status.LeaderId.Should().Be(1);
    }
}
