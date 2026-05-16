using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using RaftSimulator.Abstractions;
using RaftSimulator.Models.Domain;

using Spectre.Console;

namespace RaftSimulator.Presentation;

/// <summary>
/// Spectre.Console-based raft logger.
/// </summary>
internal sealed partial class SpectreRaftLog : IRaftLog
{
    private const int MAX_LOG_LINES = 200;
    private static readonly TimeSpan _minRenderInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Initializes a new instance of the <see cref="SpectreRaftLog"/> class.
    /// </summary>
    public SpectreRaftLog()
        : this(AnsiConsole.Console)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpectreRaftLog"/> class.
    /// </summary>
    /// <param name="console">Console implementation.</param>
    internal SpectreRaftLog(IAnsiConsole console)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }

    /// <inheritdoc />
    public void WriteNode(int nodeId, string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var time = DateTimeOffset.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var formattedMessage = HighlightNodes(message);
        if (IsQuorumWarning(message))
        {
            formattedMessage = $"[bold red]{formattedMessage}[/]";
        }
        AppendLine($"[grey]{time}[/] [bold deepskyblue1][[Node {nodeId:00}]][/] {formattedMessage}");
        RenderSnapshot(force: false);
    }

    /// <inheritdoc />
    public void WriteSystem(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var time = DateTimeOffset.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var safeMessage = Markup.Escape(message);
        AppendLine($"[grey]{time}[/] [bold yellow][[System]][/] {safeMessage}");
        RenderSnapshot(force: false);
    }

    /// <inheritdoc />
    public void WriteNodeStatus(RaftStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);

        lock (_gate)
        {
            _latestStatus = status;
        }

        RenderSnapshot(force: true);
    }

    private void AppendLine(string markup)
    {
        lock (_gate)
        {
            _lines.Add(markup);
            if (_lines.Count > MAX_LOG_LINES)
            {
                _lines.RemoveRange(0, _lines.Count - MAX_LOG_LINES);
            }
        }
    }

    private void RenderSnapshot(bool force)
    {
        Layout layout;
        Panel statusPanel;
        Panel logPanel;

        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;
            if (!force &&
                _lastRenderAt != default &&
                now - _lastRenderAt < _minRenderInterval)
            {
                return;
            }

            _lastRenderAt = now;
            statusPanel = BuildStatusPanel();
            var maxLines = GetLogLineLimit();
            logPanel = BuildLogPanel(maxLines);

            layout = new Layout("root")
                .SplitRows(
                    new Layout("status").Size(8),
                    new Layout("logs"));

            _ = layout["status"].Update(statusPanel);
            _ = layout["logs"].Update(logPanel);

            _console.Clear();
            _console.Write(layout);
        }
    }

    private Panel BuildStatusPanel()
    {
        if (_latestStatus is null)
        {
            return new Panel(new Markup("[grey]No election result yet.[/]"))
                .Header("[bold red]Election Result[/]", Justify.Left)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Red)
                .Collapse();
        }

        var status = _latestStatus;
        var grid = new Grid();
        _ = grid.AddColumn(new GridColumn().NoWrap());
        _ = grid.AddColumn();
        _ = grid.AddRow("[bold yellow]Node[/]", $"[cyan]{status.NodeId:00}[/]");
        _ = grid.AddRow("[bold yellow]Term[/]", $"[white]{status.Term}[/]");
        _ = grid.AddRow("[bold yellow]Role[/]", FormatRole(status.Role));
        _ = grid.AddRow("[bold yellow]Leader[/]", FormatLeader(status.LeaderId?.Value));

        var panel = new Panel(grid)
            .Header("[bold red]Election Result[/]", Justify.Left)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Red)
            .Collapse();

        return panel;
    }

    private Panel BuildLogPanel(int maxLines)
    {
        var content = _lines.Count == 0
            ? "[grey]No log entries yet.[/]"
            : string.Join(Environment.NewLine, _lines.TakeLast(maxLines));

        return new Panel(new Markup(content))
            .Header("[bold deepskyblue1]Log[/]", Justify.Left)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey);
    }

    private int GetLogLineLimit()
    {
        var height = _console.Profile.Height;
        if (height <= 0)
        {
            return MAX_LOG_LINES;
        }

        const int statusHeight = 8;
        const int panelChrome = 3;

        var logHeight = height - statusHeight - 1;
        var maxLines = logHeight - panelChrome;
        return Math.Clamp(maxLines, 1, MAX_LOG_LINES);
    }

    private static string FormatRole(RaftRole role) => role switch
    {
        RaftRole.Leader => "[bold red]Leader[/]",
        RaftRole.Candidate => "[yellow]Candidate[/]",
        RaftRole.Follower => "[green]Follower[/]",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown role.")
    };

    private static string FormatLeader(int? leaderId) => leaderId is null
        ? "[grey]unknown[/]"
        : $"[cyan]{leaderId:00}[/]";

    private static string HighlightNodes(string message)
    {
        var matches = _highlightToken.Matches(message);
        if (matches.Count == 0)
        {
            return Markup.Escape(message);
        }

        var builder = new StringBuilder(message.Length + (matches.Count * 8));
        var lastIndex = 0;

        foreach (Match match in matches)
        {
            if (match.Index > lastIndex)
            {
                _ = builder.Append(Markup.Escape(message[lastIndex..match.Index]));
            }

            _ = builder.Append("[bold yellow]");
            _ = builder.Append(Markup.Escape(match.Value));
            _ = builder.Append("[/]");
            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < message.Length)
        {
            _ = builder.Append(Markup.Escape(message[lastIndex..]));
        }

        return builder.ToString();
    }

    private static bool IsQuorumWarning(string message) =>
        message.StartsWith("Cluster out of quorum", StringComparison.OrdinalIgnoreCase);

    private readonly IAnsiConsole _console;
    private readonly List<string> _lines = [];
    private RaftStatus? _latestStatus;
    private DateTimeOffset _lastRenderAt;
    private static readonly Lock _gate = new();
    private static readonly Regex _highlightToken = HighlightTokenRegex();

    [GeneratedRegex(@"Node\s+\d{2}|\bterm\s+\d+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex HighlightTokenRegex();
}
