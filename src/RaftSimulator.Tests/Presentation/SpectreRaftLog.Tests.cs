using FluentAssertions;

using RaftSimulator.Models.Domain;
using RaftSimulator.Presentation;

using Spectre.Console.Testing;

namespace RaftSimulator.Tests.Presentation;

public sealed class SpectreRaftLogTests
{
    [Fact(DisplayName = "WriteNode highlights node and quorum warning")]
    [Trait("Category", "Unit")]
    public void WriteNodeHighlightsNodeAndQuorumWarning()
    {
        // Arrange
        using var console = new TestConsole();
        var log = new SpectreRaftLog(console);

        // Act
        log.WriteNode(1, "Cluster out of quorum: Node 02 term 3.");

        // Assert
        console.Output.Should().Contain("Node 01");
        console.Output.Should().Contain("Cluster out of quorum");
        console.Output.Should().Contain("Node 02");
        console.Output.Should().Contain("term 3");
    }

    [Fact(DisplayName = "WriteNode escapes plain markup")]
    [Trait("Category", "Unit")]
    public void WriteNodeEscapesPlainMarkup()
    {
        // Arrange
        using var console = new TestConsole();
        var log = new SpectreRaftLog(console);

        // Act
        log.WriteNode(1, "[red]unsafe[/]");

        // Assert
        console.Output.Should().Contain("[red]unsafe[/]");
    }

    [Fact(DisplayName = "WriteNode throttles repeated renders")]
    [Trait("Category", "Unit")]
    public void WriteNodeThrottlesRepeatedRenders()
    {
        // Arrange
        using var console = new TestConsole();
        var log = new SpectreRaftLog(console);

        // Act
        log.WriteNode(1, "first");
        var output = console.Output;
        log.WriteNode(1, "second");

        // Assert
        console.Output.Should().Be(output);
    }

    [Fact(DisplayName = "WriteNode keeps only latest log lines")]
    [Trait("Category", "Unit")]
    public void WriteNodeKeepsOnlyLatestLogLines()
    {
        // Arrange
        using var console = new TestConsole();
        var log = new SpectreRaftLog(console);

        // Act
        for (var index = 0; index < 201; index++)
        {
            log.WriteSystem($"line-{index:000}");
        }

        log.WriteNodeStatus(new RaftStatus(1, 1, RaftRole.Follower, null));

        // Assert
        console.Output.Should().Contain("line-200");
    }

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

    [Fact(DisplayName = "WriteNodeStatus formats roles")]
    [Trait("Category", "Unit")]
    public void WriteNodeStatusFormatsRoles()
    {
        // Arrange
        using var console = new TestConsole();
        var log = new SpectreRaftLog(console);

        // Act
        log.WriteNodeStatus(new RaftStatus(2, 3, RaftRole.Follower, null));
        log.WriteNodeStatus(new RaftStatus(2, 3, RaftRole.Candidate, null));
        log.WriteNodeStatus(new RaftStatus(2, 3, RaftRole.Leader, null));

        // Assert
        console.Output.Should().Contain("Follower");
        console.Output.Should().Contain("Candidate");
        console.Output.Should().Contain("Leader");
        console.Output.Should().Contain("unknown");
    }

    [Fact(DisplayName = "WriteNodeStatus rejects unknown role")]
    [Trait("Category", "Unit")]
    public void WriteNodeStatusRejectsUnknownRole()
    {
        // Arrange
        using var console = new TestConsole();
        var log = new SpectreRaftLog(console);

        // Act
        var act = () => log.WriteNodeStatus(new RaftStatus(2, 3, (RaftRole)999, null));

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
