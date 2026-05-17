using FluentAssertions;

using RaftSimulator.Abstractions;
using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;

namespace RaftSimulator.Tests.Abstractions;

public sealed class PeerRpcResultTests
{
    [Fact(DisplayName = "Success creates result with response")]
    [Trait("Category", "Unit")]
    public void SuccessCreatesResultWithResponse()
    {
        // Arrange
        var peer = new PeerInfo(2, new Uri("http://localhost:5002"));
        var response = new RaftVoteResponse(1, 2, true);

        // Act
        var result = PeerRpcResult<RaftVoteResponse>.Success(peer, response);

        // Assert
        result.Peer.Should().Be(peer);
        result.Response.Should().Be(response);
        result.Error.Should().BeNull();
    }

    [Fact(DisplayName = "Unavailable creates result without response or error")]
    [Trait("Category", "Unit")]
    public void UnavailableCreatesResultWithoutResponseOrError()
    {
        // Arrange
        var peer = new PeerInfo(2, new Uri("http://localhost:5002"));

        // Act
        var result = PeerRpcResult<RaftVoteResponse>.Unavailable(peer);

        // Assert
        result.Peer.Should().Be(peer);
        result.Response.Should().BeNull();
        result.Error.Should().BeNull();
    }

    [Fact(DisplayName = "Failed creates result with error")]
    [Trait("Category", "Unit")]
    public void FailedCreatesResultWithError()
    {
        // Arrange
        var peer = new PeerInfo(2, new Uri("http://localhost:5002"));
        var exception = new InvalidOperationException("boom");

        // Act
        var result = PeerRpcResult<RaftVoteResponse>.Failed(peer, exception);

        // Assert
        result.Peer.Should().Be(peer);
        result.Response.Should().BeNull();
        result.Error.Should().Be(exception);
    }
}
