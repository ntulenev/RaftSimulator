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

        var group = app.MapGroup("/raft");

        _ = group.MapPost("/request-vote", async (
                    RaftVoteRequestDto request,
                    IRaftNode node,
                    CancellationToken cancellationToken) =>
                {
                    var response = await node
                        .OnRequestVoteAsync(RaftDtoMapper.ToDomain(request), cancellationToken)
                        .ConfigureAwait(false);
                    return Results.Ok(RaftDtoMapper.ToDto(response));
                })
            .AddEndpointFilter<RequestVoteValidationFilter>()
            .WithName("RaftRequestVote");

        _ = group.MapPost("/append-entries", async (
                    RaftAppendEntriesRequestDto request,
                    IRaftNode node,
                    CancellationToken cancellationToken) =>
                {
                    var response = await node
                        .OnAppendEntriesAsync(RaftDtoMapper.ToDomain(request), cancellationToken)
                        .ConfigureAwait(false);
                    return Results.Ok(RaftDtoMapper.ToDto(response));
                })
            .AddEndpointFilter<AppendEntriesValidationFilter>()
            .WithName("RaftAppendEntries");

        _ = group.MapGet("/status", (IRaftNode node) =>
                Results.Ok(RaftDtoMapper.ToDto(node.GetStatus())))
            .WithName("RaftStatus");

        return app;
    }
}
