using RaftSimulator.Models.Configuration;

namespace RaftSimulator.Tests.TestSupport;

internal static class TestPeerFactory
{
    public static PeerInfo Create(int id) =>
        new(id, new Uri($"http://localhost:500{id}"));
}
