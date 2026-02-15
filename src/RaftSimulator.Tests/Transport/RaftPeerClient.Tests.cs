using System.Net;
using System.Text;
using System.Text.Json;

using FluentAssertions;

using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;
using RaftSimulator.Transport;
using RaftSimulator.Transport.Models;

namespace RaftSimulator.Tests.Transport;

public sealed class RaftPeerClientTests
{
    [Fact(DisplayName = "RequestVote returns null on non-success response")]
    [Trait("Category", "Unit")]
    public async Task RequestVoteWhenResponseIsNotSuccessReturnsNull()
    {
        // Arrange
        using var handler = new StubHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using var http = new HttpClient(handler);
        var client = new RaftPeerClient(http);
        var peer = new PeerInfo(2, new Uri("http://localhost:5002/"));

        // Act
        var response = await client
            .RequestVoteAsync(peer, new RaftVoteRequest(1, 1), CancellationToken.None);

        // Assert
        response.Should().BeNull();
    }

    [Fact(DisplayName = "RequestVote maps successful response")]
    [Trait("Category", "Unit")]
    public async Task RequestVoteMapsSuccessfulResponse()
    {
        // Arrange
        var payload = new RaftVoteResponseDto(2, 3, true);
        using var handler = new StubHandler(_ => CreateJsonResponse(payload));
        using var http = new HttpClient(handler);
        var client = new RaftPeerClient(http);
        var peer = new PeerInfo(3, new Uri("http://localhost:5003/"));

        // Act
        var response = await client
            .RequestVoteAsync(peer, new RaftVoteRequest(2, 1), CancellationToken.None);

        // Assert
        response.Should().Be(new RaftVoteResponse(2, 3, true));
        handler.LastRequest?.RequestUri.Should().Be(peer.RequestVoteUrl);
    }

    [Fact(DisplayName = "AppendEntries maps successful response")]
    [Trait("Category", "Unit")]
    public async Task AppendEntriesMapsSuccessfulResponse()
    {
        // Arrange
        var payload = new RaftAppendEntriesResponseDto(3, 2, true);
        using var handler = new StubHandler(_ => CreateJsonResponse(payload));
        using var http = new HttpClient(handler);
        var client = new RaftPeerClient(http);
        var peer = new PeerInfo(2, new Uri("http://localhost:5002/"));

        // Act
        var response = await client
            .AppendEntriesAsync(peer, new RaftAppendEntriesRequest(3, 1), CancellationToken.None);

        // Assert
        response.Should().Be(new RaftAppendEntriesResponse(3, 2, true));
        handler.LastRequest?.RequestUri.Should().Be(peer.AppendEntriesUrl);
    }

    private static HttpResponseMessage CreateJsonResponse<T>(T payload)
    {
        var json = JsonSerializer.Serialize(payload);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_handler(request));
        }
    }
}
