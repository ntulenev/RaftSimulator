using FluentAssertions;

using Microsoft.Extensions.Options;

using RaftSimulator.Models.Configuration;

namespace RaftSimulator.Tests.Models.Configuration;

public sealed class RaftOptionsValidatorTests
{
    [Fact(DisplayName = "Validate succeeds for valid options")]
    [Trait("Category", "Unit")]
    public void ValidateWhenOptionsAreValidSucceeds()
    {
        // Arrange
        var validator = new RaftOptionsValidator();

        // Act
        var result = validator.Validate(null, CreateValidOptions());

        // Assert
        result.Should().Be(ValidateOptionsResult.Success);
    }

    [Fact(DisplayName = "Validate returns all option failures")]
    [Trait("Category", "Unit")]
    public void ValidateWhenOptionsAreInvalidReturnsFailures()
    {
        // Arrange
        var options = CreateValidOptions();
        options.HeartbeatSeconds = 0;
        options.MinElectionSeconds = 5;
        options.MaxElectionSeconds = 4;
        options.MinNetworkDelaySeconds = 2;
        options.MaxNetworkDelaySeconds = 1;
        var validator = new RaftOptionsValidator();

        // Act
        var result = validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain("The field HeartbeatSeconds must be between 1 and 60.");
        result.Failures.Should().Contain("Raft:MaxElectionSeconds must be >= Raft:MinElectionSeconds.");
        result.Failures.Should().Contain(
            "Raft:MaxNetworkDelaySeconds must be >= Raft:MinNetworkDelaySeconds.");
    }

    private static RaftOptions CreateValidOptions() =>
        new()
        {
            NodeId = 1,
            Port = 5001,
            Peers = "1=http://localhost:5001;2=http://localhost:5002;3=http://localhost:5003",
            HeartbeatSeconds = 1,
            MinElectionSeconds = 4,
            MaxElectionSeconds = 7,
            MinNetworkDelaySeconds = 0,
            MaxNetworkDelaySeconds = 0
        };
}
