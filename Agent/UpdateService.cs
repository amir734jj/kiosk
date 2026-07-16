using Serilog;
using Velopack;
using Velopack.Sources;

namespace Kiosk.Agent;

public sealed class UpdateService(string? feedUrl)
{
    public async Task CheckAndApplyAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(feedUrl))
        {
            return;
        }

        try
        {
            var manager = new UpdateManager(CreateSource(feedUrl));

            if (!manager.IsInstalled)
            {
                Log.Debug("Not a Velopack install — skipping update check");
                return;
            }

            var updates = await manager.CheckForUpdatesAsync();
            if (updates is null)
            {
                Log.Debug("No update available (current {Version})", manager.CurrentVersion);
                return;
            }

            var version = updates.TargetFullRelease.Version;
            Log.Information("Update available: {Version} — downloading", version);
            await manager.DownloadUpdatesAsync(updates);

            Log.Information("Update {Version} downloaded — applying and restarting", version);
            manager.ApplyUpdatesAndRestart(updates);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Update check failed");
        }
    }

    /// <summary>GitHub Releases feed for github.com URLs, otherwise a static web feed.</summary>
    private static IUpdateSource CreateSource(string feedUrl) =>
        feedUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase)
            ? new GithubSource(feedUrl, accessToken: null, prerelease: false)
            : new SimpleWebSource(feedUrl);
}

