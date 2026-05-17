using FluentAssertions;

using RaftSimulator.Models.Domain;
using RaftSimulator.Models.Domain.Events;

namespace RaftSimulator.Tests.Models.Domain;

public sealed class DomainValueGuardTests
{
    [Fact(DisplayName = "RequirePositiveId returns valid id")]
    [Trait("Category", "Unit")]
    public void RequirePositiveIdReturnsValidId()
    {
        // Act
        var value = DomainValueGuard.RequirePositiveId(1, "id", "Node id");

        // Assert
        value.Should().Be(1);
    }

    [Theory(DisplayName = "RequirePositiveId rejects non-positive id")]
    [Trait("Category", "Unit")]
    [InlineData(0)]
    [InlineData(-1)]
    public void RequirePositiveIdRejectsNonPositiveId(int id)
    {
        // Act
        var act = () => DomainValueGuard.RequirePositiveId(id, "id", "Node id");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(id));
    }

    [Fact(DisplayName = "RequireNonNegativeTerm returns valid term")]
    [Trait("Category", "Unit")]
    public void RequireNonNegativeTermReturnsValidTerm()
    {
        // Act
        var value = DomainValueGuard.RequireNonNegativeTerm(0, "term");

        // Assert
        value.Should().Be(0);
    }

    [Fact(DisplayName = "RequireNonNegativeTerm rejects negative term")]
    [Trait("Category", "Unit")]
    public void RequireNonNegativeTermRejectsNegativeTerm()
    {
        // Act
        var act = () => DomainValueGuard.RequireNonNegativeTerm(-1, "term");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("term");
    }

    [Fact(DisplayName = "RequireTerm returns valid term")]
    [Trait("Category", "Unit")]
    public void RequireTermReturnsValidTerm()
    {
        // Act
        var term = DomainEventGuard.RequireTerm(0, "term");

        // Assert
        term.Should().Be(0);
    }

    [Fact(DisplayName = "RequireNodeId returns valid node id")]
    [Trait("Category", "Unit")]
    public void RequireNodeIdReturnsValidNodeId()
    {
        // Act
        var nodeId = DomainEventGuard.RequireNodeId(1, "nodeId", "Node id");

        // Assert
        nodeId.Should().Be(1);
    }

    [Fact(DisplayName = "RequirePositiveCount returns valid count")]
    [Trait("Category", "Unit")]
    public void RequirePositiveCountReturnsValidCount()
    {
        // Act
        var count = DomainEventGuard.RequirePositiveCount(1, "count", "Count");

        // Assert
        count.Should().Be(1);
    }

    [Fact(DisplayName = "RequireTerm rejects negative term")]
    [Trait("Category", "Unit")]
    public void RequireTermRejectsNegativeTerm()
    {
        // Act
        var act = () => DomainEventGuard.RequireTerm(-1, "term");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory(DisplayName = "RequireNodeId rejects non-positive node id")]
    [Trait("Category", "Unit")]
    [InlineData(0)]
    [InlineData(-1)]
    public void RequireNodeIdRejectsNonPositiveNodeId(int nodeId)
    {
        // Act
        var act = () => DomainEventGuard.RequireNodeId(nodeId, "nodeId", "Node id");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory(DisplayName = "RequirePositiveCount rejects non-positive count")]
    [Trait("Category", "Unit")]
    [InlineData(0)]
    [InlineData(-1)]
    public void RequirePositiveCountRejectsNonPositiveCount(int count)
    {
        // Act
        var act = () => DomainEventGuard.RequirePositiveCount(count, "count", "Count");

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
