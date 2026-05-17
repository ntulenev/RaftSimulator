using FluentAssertions;

using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;
using RaftSimulator.Transport;

namespace RaftSimulator.Tests.Transport;

public sealed class PeerRpcResultFactoryTests
{
    [Fact(DisplayName = "FromResponse maps payload to success")]
    [Trait("Category", "Unit")]
    public void FromResponseMapsPayloadToSuccess()
    {
        // Arrange
        var peer = new PeerInfo(2, new Uri("http://localhost:5002"));
        var response = new RaftVoteResponse(new Term(1), new FromId(2), true);

        // Act
        var result = PeerRpcResultFactory.FromResponse(peer, response);

        // Assert
        result.Peer.Should().Be(peer);
        result.Response.Should().Be(response);
        result.Error.Should().BeNull();
    }

    [Fact(DisplayName = "FromResponse maps null to unavailable")]
    [Trait("Category", "Unit")]
    public void FromResponseMapsNullToUnavailable()
    {
        // Arrange
        var peer = new PeerInfo(2, new Uri("http://localhost:5002"));

        // Act
        var result = PeerRpcResultFactory.FromResponse<RaftVoteResponse>(peer, null);

        // Assert
        result.Peer.Should().Be(peer);
        result.Response.Should().BeNull();
        result.Error.Should().BeNull();
    }

    [Fact(DisplayName = "FromException maps requested cancellation to unavailable")]
    [Trait("Category", "Unit")]
    public void FromExceptionMapsRequestedCancellationToUnavailable()
    {
        // Arrange
        var peer = new PeerInfo(2, new Uri("http://localhost:5002"));
        using var source = new CancellationTokenSource();
        source.Cancel();

        // Act
        var result = PeerRpcResultFactory.FromException<RaftVoteResponse>(
            peer,
            new OperationCanceledException(source.Token),
            source.Token);

        // Assert
        result.Peer.Should().Be(peer);
        result.Response.Should().BeNull();
        result.Error.Should().BeNull();
    }

    [Fact(DisplayName = "FromException maps transport exception to failure")]
    [Trait("Category", "Unit")]
    public void FromExceptionMapsTransportExceptionToFailure()
    {
        // Arrange
        var peer = new PeerInfo(2, new Uri("http://localhost:5002"));
        var exception = new HttpRequestException("offline");

        // Act
        var result = PeerRpcResultFactory.FromException<RaftVoteResponse>(
            peer,
            exception,
            CancellationToken.None);

        // Assert
        result.Peer.Should().Be(peer);
        result.Response.Should().BeNull();
        result.Error.Should().Be(exception);
    }
}
