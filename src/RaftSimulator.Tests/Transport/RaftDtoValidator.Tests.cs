using FluentAssertions;

using RaftSimulator.Transport;
using RaftSimulator.Transport.Models;

namespace RaftSimulator.Tests.Transport;

public sealed class RaftDtoValidatorTests
{
    [Fact(DisplayName = "TryValidate accepts valid vote request DTO")]
    [Trait("Category", "Unit")]
    public void TryValidateAcceptsValidVoteRequestDto()
    {
        // Act
        var valid = RaftDtoValidator.TryValidate(new RaftVoteRequestDto(1, 2), out var error);

        // Assert
        valid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Theory(DisplayName = "TryValidate rejects invalid vote request DTO")]
    [Trait("Category", "Unit")]
    [InlineData(0, 2, "Term must be positive.")]
    [InlineData(-1, 2, "Term must be positive.")]
    [InlineData(1, 0, "CandidateId must be positive.")]
    [InlineData(1, -1, "CandidateId must be positive.")]
    public void TryValidateRejectsInvalidVoteRequestDto(
        int term,
        int candidateId,
        string expectedError)
    {
        // Act
        var valid = RaftDtoValidator.TryValidate(
            new RaftVoteRequestDto(term, candidateId),
            out var error);

        // Assert
        valid.Should().BeFalse();
        error.Should().Be(expectedError);
    }

    [Fact(DisplayName = "TryValidate accepts valid append entries request DTO")]
    [Trait("Category", "Unit")]
    public void TryValidateAcceptsValidAppendEntriesRequestDto()
    {
        // Act
        var valid = RaftDtoValidator.TryValidate(
            new RaftAppendEntriesRequestDto(1, 2),
            out var error);

        // Assert
        valid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Theory(DisplayName = "TryValidate rejects invalid append entries request DTO")]
    [Trait("Category", "Unit")]
    [InlineData(0, 2, "Term must be positive.")]
    [InlineData(-1, 2, "Term must be positive.")]
    [InlineData(1, 0, "LeaderId must be positive.")]
    [InlineData(1, -1, "LeaderId must be positive.")]
    public void TryValidateRejectsInvalidAppendEntriesRequestDto(
        int term,
        int leaderId,
        string expectedError)
    {
        // Act
        var valid = RaftDtoValidator.TryValidate(
            new RaftAppendEntriesRequestDto(term, leaderId),
            out var error);

        // Assert
        valid.Should().BeFalse();
        error.Should().Be(expectedError);
    }

    [Fact(DisplayName = "TryValidate rejects null request DTOs")]
    [Trait("Category", "Unit")]
    public void TryValidateRejectsNullRequestDtos()
    {
        // Act
        Action[] acts =
        [
            () => _ = RaftDtoValidator.TryValidate((RaftVoteRequestDto)null!, out _),
            () => _ = RaftDtoValidator.TryValidate((RaftAppendEntriesRequestDto)null!, out _)
        ];

        // Assert
        foreach (var act in acts)
        {
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
