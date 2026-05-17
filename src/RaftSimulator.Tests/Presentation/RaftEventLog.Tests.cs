using Moq;

using RaftSimulator.Abstractions;
using RaftSimulator.Models.Domain.Events;
using RaftSimulator.Models.Domain;
using RaftSimulator.Presentation;

namespace RaftSimulator.Tests.Presentation;

public sealed class RaftEventLogTests
{
    [Fact(DisplayName = "WriteNodeEvent formats event before writing")]
    [Trait("Category", "Unit")]
    public void WriteNodeEventFormatsEventBeforeWriting()
    {
        // Arrange
        var log = new Mock<IRaftLog>(MockBehavior.Strict);
        var eventLog = new RaftEventLog(log.Object);

        log.Setup(logger => logger.WriteNode(1, "Became leader for term 3."));

        // Act
        eventLog.WriteNodeEvent(1, new BecameLeaderEvent(new Term(3)));

        // Assert
        log.VerifyAll();
    }
}
