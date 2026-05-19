using RaftSimulator.Abstractions;
using RaftSimulator.Logic;
using RaftSimulator.Models.Configuration;
using RaftSimulator.Presentation;
using RaftSimulator.Transport;

using Microsoft.Extensions.Options;

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

        var raftSection = builder.Configuration.GetSection("Raft");
        var settings = RaftSettingsFactory.FromOptions(raftSection.Get<RaftOptions>() ?? new RaftOptions());

        builder.ConfigureRaftUrl(settings);
        _ = builder.Services
            .AddRaftOptions(raftSection)
            .AddRaftCoreServices()
            .AddRaftHttpClient();

        return settings;
    }

    private static void ConfigureRaftUrl(this WebApplicationBuilder builder, RaftSettings settings) =>
        _ = builder.WebHost.UseUrls($"http://localhost:{settings.Port}");

    private static IServiceCollection AddRaftOptions(
        this IServiceCollection services,
        IConfiguration raftSection)
    {
        _ = services.Configure<RaftOptions>(raftSection);
        _ = services.AddSingleton<IValidateOptions<RaftOptions>, RaftOptionsValidator>();
        _ = services.AddSingleton(sp =>
            RaftSettingsFactory.FromOptions(sp.GetRequiredService<IOptions<RaftOptions>>().Value));

        return services;
    }

    private static IServiceCollection AddRaftCoreServices(this IServiceCollection services)
    {
        _ = services.AddSingleton<IRaftClock, SystemRaftClock>();
        _ = services.AddSingleton<IRaftRandom, CryptoRaftRandom>();
        _ = services.AddSingleton<IRaftDelayProvider, RaftDelayProvider>();
        _ = services.AddSingleton<IRaftScheduler, RaftScheduler>();
        _ = services.AddSingleton<IRaftNodeRuntime, RaftNodeRuntime>();
        _ = services.AddSingleton<RaftNodeCoordinator>();
        _ = services.AddSingleton<IRaftLog, SpectreRaftLog>();
        _ = services.AddSingleton<IRaftEventLog, RaftEventLog>();
        _ = services.AddSingleton<IRaftPeerBroadcaster, RaftPeerBroadcaster>();
        _ = services.AddSingleton<IRaftElectionRunner, RaftElectionRunner>();
        _ = services.AddSingleton<IRaftHeartbeatRunner, RaftHeartbeatRunner>();
        _ = services.AddSingleton<IRaftNode, RaftNode>();
        _ = services.AddHostedService<RaftHostedService>();

        return services;
    }

    private static IServiceCollection AddRaftHttpClient(this IServiceCollection services)
    {
        _ = services.AddHttpClient<IRaftPeerClient, RaftPeerClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<RaftSettings>();
            var maxTimeoutMs = settings.MinElectionTimeout.TotalMilliseconds / 2;
            var timeoutMs = Math.Min(settings.HeartbeatInterval.TotalMilliseconds, maxTimeoutMs);
            client.Timeout = TimeSpan.FromMilliseconds(Math.Max(200, timeoutMs));
        });

        return services;
    }
}
