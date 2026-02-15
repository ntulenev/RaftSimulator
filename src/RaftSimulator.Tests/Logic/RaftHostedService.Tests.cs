using FluentAssertions;

using RaftSimulator.Abstractions;
using RaftSimulator.Logic;
using RaftSimulator.Models.Domain;

namespace RaftSimulator.Tests.Logic;

public sealed class RaftHostedServiceTests
{
    [Fact(DisplayName = "ExecuteAsync delegates to node RunAsync")]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsyncDelegatesToNode()
    {
        // Arrange
        var node = new SpyNode();
        using var service = new RaftHostedService(node);

        // Act
        var method = typeof(RaftHostedService).GetMethod(
            "ExecuteAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        method.Should().NotBeNull();
        var task = (Task?)method!.Invoke(service, [CancellationToken.None]);
        task.Should().NotBeNull();
        await task!;

        // Assert
        node.RunCalls.Should().Be(1);
    }

    private sealed class SpyNode : IRaftNode
    {
        public int RunCalls { get; private set; }

        public int Id => 1;

        public Task RunAsync(CancellationToken cancellationToken)
        {
            RunCalls++;
            return Task.CompletedTask;
        }

        public Task<RaftVoteResponse> OnRequestVoteAsync(
            RaftVoteRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<RaftAppendEntriesResponse> OnAppendEntriesAsync(
            RaftAppendEntriesRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public RaftStatus GetStatus() =>
            throw new NotSupportedException();
    }
}
