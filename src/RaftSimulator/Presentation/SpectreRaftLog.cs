using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using RaftSimulator.Abstractions;
using RaftSimulator.Models.Domain;

using Spectre.Console;
using Spectre.Console.Rendering;

namespace RaftSimulator.Presentation;

/// <summary>
/// Spectre.Console-based raft logger.
/// </summary>
internal sealed partial class SpectreRaftLog : IRaftLog
{
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
        WriteLine($"[grey]{time}[/] [bold deepskyblue1][[Node {nodeId:00}]][/] {formattedMessage}");
    }

    /// <inheritdoc />
    public void WriteSystem(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var time = DateTimeOffset.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var safeMessage = Markup.Escape(message);
        WriteLine($"[grey]{time}[/] [bold yellow][[System]][/] {safeMessage}");
    }

    /// <inheritdoc />
    public void WriteNodeStatus(RaftStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);

        var grid = new Grid();
        _ = grid.AddColumn(new GridColumn().NoWrap());
        _ = grid.AddColumn();
        _ = grid.AddRow("[bold yellow]Node[/]", $"[cyan]{status.NodeId:00}[/]");
        _ = grid.AddRow("[bold yellow]Term[/]", $"[white]{status.Term}[/]");
        _ = grid.AddRow("[bold yellow]Role[/]", FormatRole(status.Role));
        _ = grid.AddRow("[bold yellow]Leader[/]", FormatLeader(status.LeaderId));

        var panel = new Panel(grid)
            .Header("[bold red]Election Result[/]", Justify.Left)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Red)
            .Collapse();

        WriteRenderable(panel);
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

    private void WriteLine(string markup)
    {
        lock (_gate)
        {
            _console.MarkupLine(markup);
        }
    }

    private void WriteRenderable(IRenderable renderable)
    {
        lock (_gate)
        {
            _console.Write(renderable);
        }
    }

    private readonly IAnsiConsole _console;
    private static readonly Lock _gate = new();
    private static readonly Regex _highlightToken = HighlightTokenRegex();

    [GeneratedRegex(@"Node\s+\d{2}|\bterm\s+\d+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex HighlightTokenRegex();
}
