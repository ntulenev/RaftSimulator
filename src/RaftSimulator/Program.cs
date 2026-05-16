using System.Text;

using RaftSimulator.Abstractions;
using RaftSimulator.API;
using RaftSimulator.Hosting;

Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables(prefix: "RAFT_");
builder.Configuration.AddCommandLine(args);

var settings = builder.AddRaftSimulator();

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
