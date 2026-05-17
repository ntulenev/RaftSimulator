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
    /// <param name="delayProvider">Delay provider.</param>
    public RaftPeerBroadcaster(
        RaftSettings settings,
        IRaftPeerClient peerClient,
        IRaftDelayProvider delayProvider)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(peerClient);
        ArgumentNullException.ThrowIfNull(delayProvider);

        _settings = settings;
        _peerClient = peerClient;
        _delayProvider = delayProvider;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PeerRpcResult<RaftVoteResponse>>> RequestVotesAsync(
        Term term,
        CandidateId candidateId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(term);
        ArgumentNullException.ThrowIfNull(candidateId);

        return BroadcastAsync(
            peer => RequestVoteAsync(peer, term, candidateId, cancellationToken),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PeerRpcResult<RaftAppendEntriesResponse>>> SendHeartbeatsAsync(
        Term term,
        LeaderId leaderId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(term);
        ArgumentNullException.ThrowIfNull(leaderId);

        return BroadcastAsync(
            peer => AppendEntriesAsync(peer, term, leaderId, cancellationToken),
            cancellationToken);
    }

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
            throw;
        }
        catch (HttpRequestException exception)
        {
            return PeerRpcResultFactory.FromException<TResponse>(
                peer,
                exception);
        }
        catch (TaskCanceledException exception)
        {
            return PeerRpcResultFactory.FromException<TResponse>(
                peer,
                exception);
        }
        catch (InvalidOperationException exception)
        {
            return PeerRpcResultFactory.FromException<TResponse>(
                peer,
                exception);
        }
    }

    private async Task<PeerRpcResult<RaftVoteResponse>> RequestVoteAsync(
        PeerInfo peer,
        Term term,
        CandidateId candidateId,
        CancellationToken cancellationToken)
    {
        var response = await _peerClient
            .RequestVoteAsync(peer, new RaftVoteRequest(term, candidateId), cancellationToken)
            .ConfigureAwait(false);

        return PeerRpcResultFactory.FromResponse(peer, response);
    }

    private async Task<PeerRpcResult<RaftAppendEntriesResponse>> AppendEntriesAsync(
        PeerInfo peer,
        Term term,
        LeaderId leaderId,
        CancellationToken cancellationToken)
    {
        var response = await _peerClient
            .AppendEntriesAsync(peer, new RaftAppendEntriesRequest(term, leaderId), cancellationToken)
            .ConfigureAwait(false);

        return PeerRpcResultFactory.FromResponse(peer, response);
    }

    private Task DelayNetworkAsync(CancellationToken cancellationToken)
    {
        var delay = _delayProvider.GetDelay(_settings.MinNetworkDelay, _settings.MaxNetworkDelay);
        return delay > TimeSpan.Zero
            ? Task.Delay(delay, cancellationToken)
            : Task.CompletedTask;
    }

    private readonly RaftSettings _settings;
    private readonly IRaftPeerClient _peerClient;
    private readonly IRaftDelayProvider _delayProvider;
}
