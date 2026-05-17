using RaftSimulator.Abstractions;
using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;
using RaftSimulator.Contracts;

namespace RaftSimulator.Transport;

/// <summary>
/// HTTP client for raft peer communication.
/// </summary>
internal sealed class RaftPeerClient : IRaftPeerClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RaftPeerClient"/> class.
    /// </summary>
    /// <param name="http">HTTP client.</param>
    public RaftPeerClient(HttpClient http)
    {
        ArgumentNullException.ThrowIfNull(http);

        _http = http;
    }

    /// <inheritdoc />
    public async Task<RaftVoteResponse?> RequestVoteAsync(
        PeerInfo peer,
        RaftVoteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(peer);
        ArgumentNullException.ThrowIfNull(request);

        var dto = RaftDtoMapper.ToDto(request);
        using var response = await _http
            .PostAsJsonAsync(peer.RequestVoteUrl, dto, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response
            .Content
            .ReadFromJsonAsync<RaftVoteResponseDto>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return payload is null ? null : RaftDtoMapper.ToDomain(payload);
    }

    /// <inheritdoc />
    public async Task<RaftAppendEntriesResponse?> AppendEntriesAsync(
        PeerInfo peer,
        RaftAppendEntriesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(peer);
        ArgumentNullException.ThrowIfNull(request);

        var dto = RaftDtoMapper.ToDto(request);
        using var response = await _http
            .PostAsJsonAsync(peer.AppendEntriesUrl, dto, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response
            .Content
            .ReadFromJsonAsync<RaftAppendEntriesResponseDto>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return payload is null ? null : RaftDtoMapper.ToDomain(payload);
    }

    private readonly HttpClient _http;
}
