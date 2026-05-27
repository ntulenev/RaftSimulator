using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using RaftSimulator.Abstractions;
using RaftSimulator.Models.Domain;
using RaftSimulator.Contracts;
using RaftSimulator.Tests.TestSupport;

namespace RaftSimulator.Tests.Integration;

public sealed class RaftApiIntegrationTests
{
    [Fact(DisplayName = "RequestVote endpoint returns mapped response")]
    [Trait("Category", "Integration")]
    public async Task RequestVoteEndpointReturnsMappedResponse()
    {
        // Arrange
        var node = new TestRaftNode
        {
            VoteResponse = new RaftVoteResponse(new Term(2), new FromId(5), true)
        };
        await using var factory = new RaftApiFactory(node);
        using var client = factory.CreateClient();

        // Act
        using var response = await client
            .PostAsJsonAsync("/raft/request-vote", new RaftVoteRequestDto(2, 9));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<RaftVoteResponseDto>();
        payload.Should().NotBeNull();
        payload!.Should().Be(new RaftVoteResponseDto(2, 5, true));

        node.VoteRequests.Should().ContainSingle();
        var request = node.VoteRequests.First();
        request.Term.Should().Be(new Term(2));
        request.CandidateId.Should().Be(new CandidateId(9));
    }

    [Fact(DisplayName = "AppendEntries endpoint returns mapped response")]
    [Trait("Category", "Integration")]
    public async Task AppendEntriesEndpointReturnsMappedResponse()
    {
        // Arrange
        var node = new TestRaftNode
        {
            AppendEntriesResponse = new RaftAppendEntriesResponse(new Term(3), new FromId(4), true)
        };
        await using var factory = new RaftApiFactory(node);
        using var client = factory.CreateClient();

        // Act
        using var response = await client
            .PostAsJsonAsync("/raft/append-entries", new RaftAppendEntriesRequestDto(3, 7));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<RaftAppendEntriesResponseDto>();
        payload.Should().NotBeNull();
        payload!.Should().Be(new RaftAppendEntriesResponseDto(3, 4, true));

        node.AppendEntriesRequests.Should().ContainSingle();
        var request = node.AppendEntriesRequests.First();
        request.Term.Should().Be(new Term(3));
        request.LeaderId.Should().Be(new LeaderId(7));
    }

    [Fact(DisplayName = "Status endpoint returns current status")]
    [Trait("Category", "Integration")]
    public async Task StatusEndpointReturnsCurrentStatus()
    {
        // Arrange
        var node = new TestRaftNode
        {
            Status = new RaftStatus(new NodeId(1), new Term(4), RaftRole.Leader, new LeaderId(1))
        };
        await using var factory = new RaftApiFactory(node);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/raft/status", UriKind.Relative));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<RaftStatusDto>();
        payload.Should().NotBeNull();
        payload!.Should().Be(new RaftStatusDto(1, 4, "Leader", 1));
    }

    [Fact(DisplayName = "RequestVote endpoint rejects invalid request")]
    [Trait("Category", "Integration")]
    public async Task RequestVoteEndpointRejectsInvalidRequest()
    {
        // Arrange
        await using var factory = new RaftApiFactory(new TestRaftNode());
        using var client = factory.CreateClient();

        // Act
        using var response = await client
            .PostAsJsonAsync("/raft/request-vote", new RaftVoteRequestDto(0, 9));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "AppendEntries endpoint rejects invalid request")]
    [Trait("Category", "Integration")]
    public async Task AppendEntriesEndpointRejectsInvalidRequest()
    {
        // Arrange
        await using var factory = new RaftApiFactory(new TestRaftNode());
        using var client = factory.CreateClient();

        // Act
        using var response = await client
            .PostAsJsonAsync("/raft/append-entries", new RaftAppendEntriesRequestDto(3, 0));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed class RaftApiFactory(TestRaftNode node) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Test");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                KeyValuePair<string, string?>[] settings =
                [
                    new("Raft:NodeId", "1"),
                    new("Raft:Port", "5001"),
                    new(
                        "Raft:Peers",
                        "1=http://localhost:5001;2=http://localhost:5002;3=http://localhost:5003"),
                    new("Raft:HeartbeatSeconds", "1"),
                    new("Raft:MinElectionSeconds", "4"),
                    new("Raft:MaxElectionSeconds", "7"),
                    new("Raft:MinNetworkDelaySeconds", "0"),
                    new("Raft:MaxNetworkDelaySeconds", "0")
                ];

                config.AddInMemoryCollection(settings);
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IRaftNode>();
                services.RemoveAll<IHostedService>();
                services.RemoveAll<IRaftLog>();

                services.AddSingleton<IRaftNode>(node);
                services.AddSingleton<IRaftLog>(new TestRaftLog());
            });
        }
    }

    private sealed class TestRaftNode : IRaftNode
    {
        private readonly ConcurrentQueue<RaftVoteRequest> _voteRequests = new();
        private readonly ConcurrentQueue<RaftAppendEntriesRequest> _appendEntriesRequests = new();

        public IReadOnlyCollection<RaftVoteRequest> VoteRequests => _voteRequests;

        public IReadOnlyCollection<RaftAppendEntriesRequest> AppendEntriesRequests =>
            _appendEntriesRequests;

        public RaftVoteResponse VoteResponse { get; init; } =
            new(new Term(1), new FromId(1), true);

        public RaftAppendEntriesResponse AppendEntriesResponse { get; init; } =
            new(new Term(1), new FromId(1), true);

        public RaftStatus Status { get; init; } =
            new(new NodeId(1), new Term(1), RaftRole.Follower, null);

        public int Id => 1;

        public Task RunAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<RaftVoteResponse> OnRequestVoteAsync(
            RaftVoteRequest request,
            CancellationToken cancellationToken)
        {
            _voteRequests.Enqueue(request);
            return Task.FromResult(VoteResponse);
        }

        public Task<RaftAppendEntriesResponse> OnAppendEntriesAsync(
            RaftAppendEntriesRequest request,
            CancellationToken cancellationToken)
        {
            _appendEntriesRequests.Enqueue(request);
            return Task.FromResult(AppendEntriesResponse);
        }

        public RaftStatus GetStatus() => Status;
    }

}
