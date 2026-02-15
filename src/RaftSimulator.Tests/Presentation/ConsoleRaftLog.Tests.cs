using FluentAssertions;

using RaftSimulator.Models.Domain;
using RaftSimulator.Presentation;

namespace RaftSimulator.Tests.Presentation;

public sealed class ConsoleRaftLogTests
{
    [Fact(DisplayName = "WriteNodeStatus writes status and restores color")]
    [Trait("Category", "Unit")]
    public void WriteNodeStatusWritesAndRestoresColor()
    {
        // Arrange
        var log = new ConsoleRaftLog();
        using var writer = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(writer);

        try
        {
            // Act
            log.WriteNodeStatus(new RaftStatus(1, 2, RaftRole.Leader, 1));

            // Assert
            var output = writer.ToString();
            output.Should().Contain("Status: id=01, role=Leader, leader=01");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact(DisplayName = "WriteSystem writes system prefix")]
    [Trait("Category", "Unit")]
    public void WriteSystemWritesPrefix()
    {
        // Arrange
        var log = new ConsoleRaftLog();
        using var writer = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(writer);

        try
        {
            // Act
            log.WriteSystem("Hello");

            // Assert
            writer.ToString().Should().Contain("[System] Hello");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
