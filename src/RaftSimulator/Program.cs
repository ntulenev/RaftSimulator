using System.Text;

using Microsoft.Extensions.Options;

using RaftSimulator.Abstractions;
using RaftSimulator.API;
using RaftSimulator.Logic;
using RaftSimulator.Models.Configuration;
using RaftSimulator.Presentation;
using RaftSimulator.Transport;

Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables(prefix: "RAFT_");
builder.Configuration.AddCommandLine(args);

builder.Services
    .AddOptions<RaftOptions>()
    .Bind(builder.Configuration.GetSection("Raft"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<RaftOptions>>().Value;
    return RaftSettings.FromOptions(options);
});

builder.Services.AddSingleton<IRaftLog, SpectreRaftLog>();
builder.Services.AddSingleton<IRaftNode, RaftNode>();
builder.Services.AddHostedService<RaftHostedService>();
builder.Services.AddHttpClient<IRaftPeerClient, RaftPeerClient>((sp, client) =>
{
    var settings = sp.GetRequiredService<RaftSettings>();
    var maxTimeoutMs = settings.MinElectionTimeout.TotalMilliseconds / 2;
    var timeoutMs = Math.Min(settings.HeartbeatInterval.TotalMilliseconds, maxTimeoutMs);
    client.Timeout = TimeSpan.FromMilliseconds(Math.Max(200, timeoutMs));
});

var settings = RaftSettings.FromOptions(
    builder.Configuration.GetSection("Raft").Get<RaftOptions>() ?? new RaftOptions());
builder.WebHost.UseUrls($"http://localhost:{settings.Port}");

var app = builder.Build();

app.MapRaftApi();

var log = app.Services.GetRequiredService<IRaftLog>();
log.WriteSystem(
    $"Node {settings.NodeId:00} starting on port {settings.Port}. " +
    $"Peers={settings.Peers.Count}, Majority={settings.Majority}.");
log.WriteSystem("Press Ctrl+C to stop.");

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    log.WriteSystem("Shutdown requested. Stopping...");
    app.Lifetime.StopApplication();
};

await app.RunAsync().ConfigureAwait(false);
