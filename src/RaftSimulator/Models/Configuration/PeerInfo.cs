namespace RaftSimulator.Models.Configuration;

/// <summary>
/// Peer configuration entry.
/// </summary>
internal sealed record PeerInfo(int Id, Uri BaseUrl)
{
    /// <summary>
    /// Gets request vote URL.
    /// </summary>
    public Uri RequestVoteUrl => new(BaseUrl, "raft/request-vote");

    /// <summary>
    /// Gets append entries URL.
    /// </summary>
    public Uri AppendEntriesUrl => new(BaseUrl, "raft/append-entries");

    /// <summary>
    /// Parses peer entry in "id=http://host:port" format.
    /// </summary>
    /// <param name="value">Raw value.</param>
    /// <returns>Peer info.</returns>
    public static PeerInfo Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException("Peer definition is empty.");
        }

        var normalized = value.Trim().Trim('\'', '"');

        var parts = normalized
            .Split('=', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            throw new FormatException(
                $"Invalid peer '{value}'. Expected format: id=http://host:port");
        }

        var idText = parts[0].Trim().Trim('\'', '"');
        if (!int.TryParse(idText, out var id) || id < 1)
        {
            throw new FormatException($"Invalid peer id '{parts[0]}'.");
        }

        var urlText = parts[1].Trim().Trim('\'', '"');
        if (!Uri.TryCreate(urlText, UriKind.Absolute, out var uri))
        {
            throw new FormatException($"Invalid peer url '{parts[1]}'.");
        }

        var normalizedUrl = uri.AbsoluteUri.EndsWith('/')
            ? uri
            : new Uri(uri.AbsoluteUri + "/", UriKind.Absolute);

        return new PeerInfo(id, normalizedUrl);
    }

    /// <summary>
    /// Parses a semicolon-separated list of peer entries.
    /// </summary>
    /// <param name="peers">Raw list.</param>
    /// <returns>Peer info list.</returns>
    public static PeerInfo[] ParseList(string? peers)
    {
        if (string.IsNullOrWhiteSpace(peers))
        {
            return [];
        }

        var normalized = peers.Trim().Trim('\'', '"');

        var entries = normalized
            .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var result = new List<PeerInfo>(entries.Length);
        var ids = new HashSet<int>();

        foreach (var entry in entries)
        {
            var peer = Parse(entry);
            if (!ids.Add(peer.Id))
            {
                throw new FormatException($"Duplicate peer id '{peer.Id}'.");
            }

            result.Add(peer);
        }

        return [.. result];
    }
}
