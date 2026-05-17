using FluentAssertions;

using RaftSimulator.Logic;
using RaftSimulator.Models.Domain;

namespace RaftSimulator.Tests.Logic;

public sealed class RaftStatusReporterTests
{
    [Fact(DisplayName = "GetSnapshotToPublish returns null when leader is unknown")]
    [Trait("Category", "Unit")]
    public void GetSnapshotToPublishWhenLeaderIsUnknownReturnsNull()
    {
        // Arrange
        var reporter = new RaftStatusReporter();

        // Act
        var snapshot = reporter.GetSnapshotToPublish(new RaftStatus(new NodeId(1), new Term(1), RaftRole.Follower, null));

        // Assert
        snapshot.Should().BeNull();
    }

    [Fact(DisplayName = "GetSnapshotToPublish returns one snapshot per term")]
    [Trait("Category", "Unit")]
    public void GetSnapshotToPublishReturnsOneSnapshotPerTerm()
    {
        // Arrange
        var reporter = new RaftStatusReporter();
        var status = new RaftStatus(new NodeId(1), new Term(1), RaftRole.Leader, new LeaderId(1));

        // Act
        var first = reporter.GetSnapshotToPublish(status);
        var second = reporter.GetSnapshotToPublish(status);

        // Assert
        first.Should().Be(status);
        second.Should().BeNull();
    }

    [Fact(DisplayName = "Reset allows publishing current term again")]
    [Trait("Category", "Unit")]
    public void ResetAllowsPublishingCurrentTermAgain()
    {
        // Arrange
        var reporter = new RaftStatusReporter();
        var status = new RaftStatus(new NodeId(1), new Term(1), RaftRole.Leader, new LeaderId(1));
        _ = reporter.GetSnapshotToPublish(status);

        // Act
        reporter.Reset();
        var snapshot = reporter.GetSnapshotToPublish(status);

        // Assert
        snapshot.Should().Be(status);
    }
}
