using FluentAssertions;

using RaftSimulator.Models.Configuration;

namespace RaftSimulator.Tests.Models.Configuration;

public sealed class PeerInfoTests
{
    [Fact(DisplayName = "ParseList returns empty list when peers are missing")]
    [Trait("Category", "Unit")]
    public void ParseListWhenPeersAreMissingReturnsEmpty()
    {
        // Act
        var peers = PeerInfo.ParseList(null);

        // Assert
        peers.Should().BeEmpty();
    }

    [Fact(DisplayName = "Parse normalizes base URL and builds endpoints")]
    [Trait("Category", "Unit")]
    public void ParseNormalizesBaseUrl()
    {
        // Act
        var peer = PeerInfo.Parse("5=http://localhost:5005");

        // Assert
        peer.Id.Should().Be(5);
        peer.BaseUrl.AbsoluteUri.Should().Be("http://localhost:5005/");
        peer.RequestVoteUrl.AbsoluteUri.Should().EndWith("/raft/request-vote");
        peer.AppendEntriesUrl.AbsoluteUri.Should().EndWith("/raft/append-entries");
    }

    [Fact(DisplayName = "Parse throws on invalid input")]
    [Trait("Category", "Unit")]
    public void ParseWhenInvalidThrows()
    {
        // Act
        var act = () => PeerInfo.Parse("bad-value");

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Fact(DisplayName = "ParseList throws on duplicate peer ids")]
    [Trait("Category", "Unit")]
    public void ParseListWhenDuplicateIdsThrows()
    {
        // Arrange
        var input = "1=http://localhost:5001;1=http://localhost:5002";

        // Act
        var act = () => PeerInfo.ParseList(input);

        // Assert
        act.Should()
            .Throw<FormatException>()
            .WithMessage("Duplicate peer id '1'.");
    }
}
