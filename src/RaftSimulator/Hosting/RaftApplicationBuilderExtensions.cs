using RaftSimulator.Abstractions;
using RaftSimulator.Logic;
using RaftSimulator.Models.Configuration;
using RaftSimulator.Presentation;
using RaftSimulator.Transport;

namespace RaftSimulator.Hosting;

/// <summary>
/// Configures the raft simulator host.
/// </summary>
internal static class RaftApplicationBuilderExtensions
{
    /// <summary>
    /// Adds raft simulator services to the application builder.
    /// </summary>
    /// <param name="builder">Application builder.</param>
    /// <returns>Resolved raft settings.</returns>
    public static RaftSettings AddRaftSimulator(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = RaftSettings.FromOptions(
            builder.Configuration.GetSection("Raft").Get<RaftOptions>() ?? new RaftOptions());
        _ = builder.WebHost.UseUrls($"http://localhost:{settings.Port}");

        _ = builder.Services.AddSingleton(settings);
        _ = builder.Services.AddSingleton<IRaftClock, SystemRaftClock>();
        _ = builder.Services.AddSingleton<IRaftRandom, CryptoRaftRandom>();
        _ = builder.Services.AddSingleton<IRaftDelayProvider, RaftDelayProvider>();
        _ = builder.Services.AddSingleton<IRaftScheduler, RaftScheduler>();
        _ = builder.Services.AddSingleton<IRaftLog, SpectreRaftLog>();
        _ = builder.Services.AddSingleton<IRaftEventLog, RaftEventLog>();
        _ = builder.Services.AddSingleton<IRaftPeerBroadcaster, RaftPeerBroadcaster>();
        _ = builder.Services.AddSingleton<IRaftElectionRunner, RaftElectionRunner>();
        _ = builder.Services.AddSingleton<IRaftHeartbeatRunner, RaftHeartbeatRunner>();
        _ = builder.Services.AddSingleton<IRaftNode, RaftNode>();
        _ = builder.Services.AddHostedService<RaftHostedService>();
        _ = builder.Services.AddHttpClient<IRaftPeerClient, RaftPeerClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<RaftSettings>();
            var maxTimeoutMs = settings.MinElectionTimeout.TotalMilliseconds / 2;
            var timeoutMs = Math.Min(settings.HeartbeatInterval.TotalMilliseconds, maxTimeoutMs);
            client.Timeout = TimeSpan.FromMilliseconds(Math.Max(200, timeoutMs));
        });

        return settings;
    }
}
