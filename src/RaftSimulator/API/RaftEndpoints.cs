using RaftSimulator.Abstractions;
using RaftSimulator.Transport;
using RaftSimulator.Transport.Models;

namespace RaftSimulator.API;

/// <summary>
/// HTTP endpoints for raft node interactions.
/// </summary>
internal static class RaftEndpoints
{
    /// <summary>
    /// Maps raft HTTP endpoints.
    /// </summary>
    /// <param name="app">Endpoint route builder.</param>
    /// <returns>Builder.</returns>
    public static IEndpointRouteBuilder MapRaftApi(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        _ = app.MapPost("/raft/request-vote", async (
                    RaftVoteRequestDto request,
                    IRaftNode node,
                    CancellationToken cancellationToken) =>
                {
                    if (!IsValid(request.Term, request.CandidateId))
                    {
                        return Results.BadRequest("Term and CandidateId must be positive.");
                    }

                    var response = await node
                        .OnRequestVoteAsync(RaftDtoMapper.ToDomain(request), cancellationToken)
                        .ConfigureAwait(false);
                    return Results.Ok(RaftDtoMapper.ToDto(response));
                })
            .WithName("RaftRequestVote");

        _ = app.MapPost("/raft/append-entries", async (
                    RaftAppendEntriesRequestDto request,
                    IRaftNode node,
                    CancellationToken cancellationToken) =>
                {
                    if (!IsValid(request.Term, request.LeaderId))
                    {
                        return Results.BadRequest("Term and LeaderId must be positive.");
                    }

                    var response = await node
                        .OnAppendEntriesAsync(RaftDtoMapper.ToDomain(request), cancellationToken)
                        .ConfigureAwait(false);
                    return Results.Ok(RaftDtoMapper.ToDto(response));
                })
            .WithName("RaftAppendEntries");

        _ = app.MapGet("/raft/status", (IRaftNode node) =>
                Results.Ok(RaftDtoMapper.ToDto(node.GetStatus())))
            .WithName("RaftStatus");

        return app;
    }

    private static bool IsValid(int term, int nodeId) =>
        term > 0 && nodeId > 0;
}
