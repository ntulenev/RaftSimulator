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

    [Fact(DisplayName = "RequirePositiveId rejects non-positive id")]
    [Trait("Category", "Unit")]
    public void RequirePositiveIdRejectsNonPositiveId()
    {
        // Act
        Action[] acts =
        [
            () => _ = DomainValueGuard.RequirePositiveId(0, "id", "Node id"),
            () => _ = DomainValueGuard.RequirePositiveId(-1, "id", "Node id")
        ];

        // Assert
        foreach (var act in acts)
        {
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithParameterName("id");
        }
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

    [Fact(DisplayName = "DomainEventGuard returns valid primitive values")]
    [Trait("Category", "Unit")]
    public void DomainEventGuardReturnsValidPrimitiveValues()
    {
        // Act
        var term = DomainEventGuard.RequireTerm(0, "term");
        var nodeId = DomainEventGuard.RequireNodeId(1, "nodeId", "Node id");
        var count = DomainEventGuard.RequirePositiveCount(1, "count", "Count");

        // Assert
        term.Should().Be(0);
        nodeId.Should().Be(1);
        count.Should().Be(1);
    }

    [Fact(DisplayName = "DomainEventGuard rejects invalid primitive values")]
    [Trait("Category", "Unit")]
    public void DomainEventGuardRejectsInvalidPrimitiveValues()
    {
        // Act
        Action[] acts =
        [
            () => _ = DomainEventGuard.RequireTerm(-1, "term"),
            () => _ = DomainEventGuard.RequireNodeId(0, "nodeId", "Node id"),
            () => _ = DomainEventGuard.RequireNodeId(-1, "nodeId", "Node id"),
            () => _ = DomainEventGuard.RequirePositiveCount(0, "count", "Count"),
            () => _ = DomainEventGuard.RequirePositiveCount(-1, "count", "Count")
        ];

        // Assert
        foreach (var act in acts)
        {
            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
