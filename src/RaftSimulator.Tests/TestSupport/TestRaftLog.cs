using RaftSimulator.Abstractions;
using RaftSimulator.Models.Domain;

namespace RaftSimulator.Tests.TestSupport;

internal sealed class TestRaftLog : IRaftLog
{
    public List<string> Messages { get; } = [];

    public List<RaftStatus> Statuses { get; } = [];

    public void WriteNode(int nodeId, string message) =>
        Messages.Add(message);

    public void WriteSystem(string message) =>
        Messages.Add(message);

    public void WriteNodeStatus(RaftStatus status) =>
        Statuses.Add(status);
}
