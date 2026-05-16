using FluentAssertions;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using RaftSimulator.Abstractions;
using RaftSimulator.Hosting;
using RaftSimulator.Logic;

namespace RaftSimulator.Tests.Hosting;

public sealed class RaftApplicationBuilderExtensionsTests
{
    [Fact(DisplayName = "AddRaftSimulator registers node orchestration services")]
    [Trait("Category", "Unit")]
    public void AddRaftSimulatorRegistersNodeOrchestrationServices()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Test"
        });
        builder.Configuration.AddInMemoryCollection(
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
        ]);

        // Act
        var settings = builder.AddRaftSimulator();
        using var provider = builder.Services.BuildServiceProvider();

        // Assert
        settings.NodeId.Should().Be(1);
        provider.GetRequiredService<IRaftElectionRunner>().Should().BeOfType<RaftElectionRunner>();
        provider.GetRequiredService<IRaftHeartbeatRunner>().Should().BeOfType<RaftHeartbeatRunner>();
        provider.GetRequiredService<IRaftNode>().Should().BeOfType<RaftNode>();
    }
}
