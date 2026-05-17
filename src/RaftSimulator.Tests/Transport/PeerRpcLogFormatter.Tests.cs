using FluentAssertions;

using RaftSimulator.Transport;

namespace RaftSimulator.Tests.Transport;

public sealed class PeerRpcLogFormatterTests
{
    [Fact(DisplayName = "FormatFailure formats transport failures as unreachable")]
    [Trait("Category", "Unit")]
    public void FormatFailureFormatsTransportFailuresAsUnreachable()
    {
        // Act
        var message = PeerRpcLogFormatter.FormatFailure(
            "RequestVote",
            2,
            3,
            new HttpRequestException("offline"));

        // Assert
        message.Should().Be("Unable to reach Node 02.");
    }

    [Fact(DisplayName = "FormatFailure includes unexpected exception details")]
    [Trait("Category", "Unit")]
    public void FormatFailureIncludesUnexpectedExceptionDetails()
    {
        // Act
        var message = PeerRpcLogFormatter.FormatFailure(
            "AppendEntries",
            2,
            3,
            new InvalidOperationException("boom"));

        // Assert
        message.Should().Be(
            "AppendEntries (term 3) -> Node 02 failed: InvalidOperationException: boom");
    }
}
