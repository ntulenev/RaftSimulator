using FluentAssertions;

using RaftSimulator.Models.Domain;
using RaftSimulator.Transport;
using RaftSimulator.Contracts;

namespace RaftSimulator.Tests.Transport;

public sealed class RaftDtoMapperTests
{
    [Fact(DisplayName = "Vote mapping preserves values")]
    [Trait("Category", "Unit")]
    public void VoteMappingPreservesValues()
    {
        // Arrange
        var request = new RaftVoteRequest(new Term(2), new CandidateId(3));
        var response = new RaftVoteResponse(new Term(2), new FromId(4), true);

        // Act
        var requestDto = RaftDtoMapper.ToDto(request);
        var requestDomain = RaftDtoMapper.ToDomain(requestDto);
        var responseDto = RaftDtoMapper.ToDto(response);
        var responseDomain = RaftDtoMapper.ToDomain(responseDto);

        // Assert
        requestDomain.Should().Be(request);
        responseDomain.Should().Be(response);
    }

    [Fact(DisplayName = "Append entries mapping preserves values")]
    [Trait("Category", "Unit")]
    public void AppendEntriesMappingPreservesValues()
    {
        // Arrange
        var request = new RaftAppendEntriesRequest(new Term(5), new LeaderId(1));
        var response = new RaftAppendEntriesResponse(new Term(5), new FromId(2), true);

        // Act
        var requestDto = RaftDtoMapper.ToDto(request);
        var requestDomain = RaftDtoMapper.ToDomain(requestDto);
        var responseDto = RaftDtoMapper.ToDto(response);
        var responseDomain = RaftDtoMapper.ToDomain(responseDto);

        // Assert
        requestDomain.Should().Be(request);
        responseDomain.Should().Be(response);
    }

    [Fact(DisplayName = "Status mapping uses role string")]
    [Trait("Category", "Unit")]
    public void StatusMappingUsesRoleString()
    {
        // Arrange
        var status = new RaftStatus(new NodeId(1), new Term(2), RaftRole.Follower, null);

        // Act
        var dto = RaftDtoMapper.ToDto(status);

        // Assert
        dto.Should().Be(new RaftStatusDto(1, 2, "Follower", null));
    }
}
