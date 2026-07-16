using Serilog;
using Shared.Contracts;

namespace Kiosk.Agent;

public sealed record AgentSettings(string BackendUrl, string FallbackDisplayUrl)
{
    private const string DefaultBackend = "https://kiosk.hesamian.com";

    public static AgentSettings Load(string[] args)
    {
        var backend = Environment.GetEnvironmentVariable("KIOSK_BACKEND_URL");
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            backend = args[0];
        }

        backend = string.IsNullOrWhiteSpace(backend) ? DefaultBackend : backend.TrimEnd('/');

        var display = Environment.GetEnvironmentVariable("KIOSK_DISPLAY_URL");
        if (string.IsNullOrWhiteSpace(display))
        {
            display = $"{backend}/display";
        }

        return new AgentSettings(backend, display);
    }

    public AgentConfigDto Fallback()
    {
        Log.Warning("Using local fallback config (backend unreachable)");
        return new AgentConfigDto(
            DisplayUrl: FallbackDisplayUrl,
            BetterStackSourceToken: Environment.GetEnvironmentVariable("BETTERSTACK_SOURCE_TOKEN"),
            BetterStackIngestingHost: Environment.GetEnvironmentVariable("BETTERSTACK_INGESTING_HOST"),
            UpdateFeedUrl: Environment.GetEnvironmentVariable("KIOSK_UPDATE_FEED_URL"),
            HealthCheckIntervalSeconds: 120,
            MaxFailBeforeRestart: 3,
            MaxRestarts: 3,
            MaxIntervalSeconds: 600,
            UpdateCheckIntervalMinutes: 60);
    }
}
