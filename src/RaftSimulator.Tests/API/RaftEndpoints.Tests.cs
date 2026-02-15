using FluentAssertions;

using RaftSimulator.API;

namespace RaftSimulator.Tests.API;

public sealed class RaftEndpointsTests
{
    [Fact(DisplayName = "MapRaftApi throws on null builder")]
    [Trait("Category", "Unit")]
    public void MapRaftApiWhenBuilderIsNullThrows()
    {
        // Arrange
        Action act = () => RaftEndpoints.MapRaftApi(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
