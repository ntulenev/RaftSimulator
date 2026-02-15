using FluentAssertions;

using RaftSimulator.Models.Domain;
using RaftSimulator.Presentation;

using Spectre.Console.Testing;

namespace RaftSimulator.Tests.Presentation;

public sealed class SpectreRaftLogTests
{
    [Fact(DisplayName = "WriteSystem writes system message")]
    [Trait("Category", "Unit")]
    public void WriteSystemWritesMessage()
    {
        // Arrange
        using var console = new TestConsole();
        var log = new SpectreRaftLog(console);

        // Act
        log.WriteSystem("Hello");

        // Assert
        console.Output.Should().Contain("System");
        console.Output.Should().Contain("Hello");
    }

    [Fact(DisplayName = "WriteNodeStatus writes election panel")]
    [Trait("Category", "Unit")]
    public void WriteNodeStatusWritesPanel()
    {
        // Arrange
        using var console = new TestConsole();
        var log = new SpectreRaftLog(console);

        // Act
        log.WriteNodeStatus(new RaftStatus(2, 3, RaftRole.Leader, 2));

        // Assert
        console.Output.Should().Contain("Election");
        console.Output.Should().Contain("Leader");
    }
}
