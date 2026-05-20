using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;
using RaftSimulator.Models.Domain.Events;

namespace RaftSimulator.Logic;

/// <summary>
/// Shared state and decision factories for raft state-machine handlers.
/// </summary>
internal sealed class RaftStateMachineContext
{
    internal RaftStateMachineContext(RaftSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Settings = settings;
    }

    internal int Id => Settings.NodeId;

    internal RaftSettings Settings { get; }

    internal RaftStatusReporter StatusReporter { get; } = new();

    internal RaftNodeState State { get; } = new();

    internal RaftStatus GetStatus() =>
        new(new NodeId(Id), State.CurrentTerm, State.Role, State.LeaderId);

    internal VoteDecision CreateVoteDecision(bool granted, IReadOnlyList<RaftEvent> events) =>
        new(new RaftVoteResponse(State.CurrentTerm, new FromId(Id), granted), events);

    internal static RaftEvent CreateVoteEvent(
        RaftVoteRequest request,
        bool granted,
        Term term) =>
        granted
            ? new RequestVoteGrantedEvent(request.CandidateId, term)
            : new RequestVoteDeniedEvent(request.CandidateId, term);

    internal AppendEntriesDecision CreateAppendEntriesDecision(
        bool success,
        IReadOnlyList<RaftEvent> events,
        RaftStatus? statusSnapshot) =>
        new(new RaftAppendEntriesResponse(State.CurrentTerm, new FromId(Id), success), events, statusSnapshot);

    internal static VoteResponseDecision CreateVoteResponseDecision(
        IReadOnlyList<RaftEvent> events,
        bool becameLeader,
        Term term,
        RaftStatus? statusSnapshot) =>
        new(events, becameLeader, term, statusSnapshot);

    internal BecameFollowerEvent BecomeFollower(
        int term,
        int? leaderId,
        DateTimeOffset now,
        TimeSpan electionTimeout)
    {
        State.BecomeFollower(term, leaderId, now, electionTimeout);
        return new BecameFollowerEvent(
            State.CurrentTerm,
            leaderId is null ? null : new LeaderId(leaderId.Value));
    }
}
