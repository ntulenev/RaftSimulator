using RaftSimulator.Abstractions;
using RaftSimulator.Models.Configuration;
using RaftSimulator.Models.Domain;

namespace RaftSimulator.Transport;

/// <summary>
/// Broadcasts raft RPC calls to all configured peers.
/// </summary>
internal sealed class RaftPeerBroadcaster : IRaftPeerBroadcaster
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RaftPeerBroadcaster"/> class.
    /// </summary>
    /// <param name="settings">Raft settings.</param>
    /// <param name="peerClient">Peer client.</param>
    /// <param name="random">Random source.</param>
    public RaftPeerBroadcaster(
        RaftSettings settings,
        IRaftPeerClient peerClient,
        IRaftRandom random)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(peerClient);
        ArgumentNullException.ThrowIfNull(random);

        _settings = settings;
        _peerClient = peerClient;
        _random = random;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PeerRpcResult<RaftVoteResponse>>> RequestVotesAsync(
        int term,
        int candidateId,
        CancellationToken cancellationToken) =>
        BroadcastAsync(
            peer => RequestVoteAsync(peer, term, candidateId, cancellationToken),
            cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<PeerRpcResult<RaftAppendEntriesResponse>>> SendHeartbeatsAsync(
        int term,
        int leaderId,
        CancellationToken cancellationToken) =>
        BroadcastAsync(
            peer => AppendEntriesAsync(peer, term, leaderId, cancellationToken),
            cancellationToken);

    private async Task<IReadOnlyList<PeerRpcResult<TResponse>>> BroadcastAsync<TResponse>(
        Func<PeerInfo, Task<PeerRpcResult<TResponse>>> sendAsync,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        var tasks = _settings
            .Peers
            .Select(peer => SendToPeerAsync(peer, sendAsync, cancellationToken));

        return await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task<PeerRpcResult<TResponse>> SendToPeerAsync<TResponse>(
        PeerInfo peer,
        Func<PeerInfo, Task<PeerRpcResult<TResponse>>> sendAsync,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        try
        {
            await DelayNetworkAsync(cancellationToken).ConfigureAwait(false);
            return await sendAsync(peer).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return PeerRpcResult<TResponse>.Unavailable(peer);
        }
        catch (HttpRequestException exception)
        {
            return PeerRpcResult<TResponse>.Failed(peer, exception);
        }
        catch (TaskCanceledException exception)
        {
            return PeerRpcResult<TResponse>.Failed(peer, exception);
        }
        catch (InvalidOperationException exception)
        {
            return PeerRpcResult<TResponse>.Failed(peer, exception);
        }
    }

    private async Task<PeerRpcResult<RaftVoteResponse>> RequestVoteAsync(
        PeerInfo peer,
        int term,
        int candidateId,
        CancellationToken cancellationToken)
    {
        var response = await _peerClient
            .RequestVoteAsync(peer, new RaftVoteRequest(term, candidateId), cancellationToken)
            .ConfigureAwait(false);

        return response is null
            ? PeerRpcResult<RaftVoteResponse>.Unavailable(peer)
            : PeerRpcResult<RaftVoteResponse>.Success(peer, response);
    }

    private async Task<PeerRpcResult<RaftAppendEntriesResponse>> AppendEntriesAsync(
        PeerInfo peer,
        int term,
        int leaderId,
        CancellationToken cancellationToken)
    {
        var response = await _peerClient
            .AppendEntriesAsync(peer, new RaftAppendEntriesRequest(term, leaderId), cancellationToken)
            .ConfigureAwait(false);

        return response is null
            ? PeerRpcResult<RaftAppendEntriesResponse>.Unavailable(peer)
            : PeerRpcResult<RaftAppendEntriesResponse>.Success(peer, response);
    }

    private Task DelayNetworkAsync(CancellationToken cancellationToken)
    {
        var delay = GetRandomDelay(_settings.MinNetworkDelay, _settings.MaxNetworkDelay);
        return delay > TimeSpan.Zero
            ? Task.Delay(delay, cancellationToken)
            : Task.CompletedTask;
    }

    private TimeSpan GetRandomDelay(TimeSpan min, TimeSpan max)
    {
        if (min == max)
        {
            return min;
        }

        var window = max - min;
        var offset = TimeSpan.FromMilliseconds(window.TotalMilliseconds * _random.NextDouble());
        return min + offset;
    }

    private readonly RaftSettings _settings;
    private readonly IRaftPeerClient _peerClient;
    private readonly IRaftRandom _random;
}
