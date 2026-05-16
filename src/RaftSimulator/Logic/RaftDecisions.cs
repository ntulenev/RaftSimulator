using RaftSimulator.Models.Domain;

namespace RaftSimulator.Logic;

internal enum TimeoutActionType
{
    Election,
    Heartbeats
}

internal sealed record TimeoutAction(TimeoutActionType Type, int Term, string LogLine);

internal sealed record VoteDecision(RaftVoteResponse Response, string LogLine);

internal sealed record AppendEntriesDecision(
    RaftAppendEntriesResponse Response,
    string LogLine,
    RaftStatus? StatusSnapshot);

internal sealed record VoteResponseDecision(
    IReadOnlyList<string> LogLines,
    bool BecameLeader,
    int Term,
    RaftStatus? StatusSnapshot);

internal sealed record AppendEntriesResponseDecision(IReadOnlyList<string> LogLines);
