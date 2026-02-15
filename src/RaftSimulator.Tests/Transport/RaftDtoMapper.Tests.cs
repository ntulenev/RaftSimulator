using FluentAssertions;

using RaftSimulator.Models.Domain;
using RaftSimulator.Transport;
using RaftSimulator.Transport.Models;

namespace RaftSimulator.Tests.Transport;

public sealed class RaftDtoMapperTests
{
    [Fact(DisplayName = "Vote mapping preserves values")]
    [Trait("Category", "Unit")]
    public void VoteMappingPreservesValues()
    {
        // Arrange
        var request = new RaftVoteRequest(2, 3);
        var response = new RaftVoteResponse(2, 4, true);

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
        var request = new RaftAppendEntriesRequest(5, 1);
        var response = new RaftAppendEntriesResponse(5, 2, true);

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
        var status = new RaftStatus(1, 2, RaftRole.Follower, null);

        // Act
        var dto = RaftDtoMapper.ToDto(status);

        // Assert
        dto.Should().Be(new RaftStatusDto(1, 2, "Follower", null));
    }
}
