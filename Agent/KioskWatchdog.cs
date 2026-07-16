using Serilog;
using Shared.Contracts;

namespace Kiosk.Agent;

public sealed class KioskWatchdog(AgentConfigDto config, ChromiumLauncher chromium, UpdateService updater, HeartbeatReporter heartbeat)
{
    public async Task RunAsync(CancellationToken ct)
    {
        PrepareDisplay();
        chromium.Launch();
        await heartbeat.SendAsync(chromium.IsRunning(), ct);

        var baseInterval = Math.Max(10, config.HealthCheckIntervalSeconds);
        var maxInterval = Math.Max(baseInterval, config.MaxIntervalSeconds);
        var currentInterval = baseInterval;

        var failCount = 0;
        var restartCount = 0;
        var wasDown = false;
        var lastUpdateCheck = DateTimeOffset.UtcNow;

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        Log.Information(
            "Watchdog started (interval {Interval}s, restart after {Fail} fails, max {Restarts} restarts)",
            baseInterval, config.MaxFailBeforeRestart, config.MaxRestarts);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(currentInterval), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            lastUpdateCheck = await MaybeCheckForUpdatesAsync(ct, lastUpdateCheck) ?? lastUpdateCheck;

            var chromiumRunning = chromium.IsRunning();
            await heartbeat.SendAsync(chromiumRunning, ct);

            if (!chromiumRunning)
            {
                Log.Warning("Chromium not running — relaunching");
                chromium.Launch();
                continue;
            }

            var httpCode = await HealthCheckAsync(http, config.DisplayUrl, ct);

            if (httpCode is >= 200 and < 400)
            {
                if (wasDown)
                {
                    Log.Information("Site is back (HTTP {Code}) — refreshing page", httpCode);
                    chromium.Refresh();
                }

                failCount = 0;
                restartCount = 0;
                currentInterval = baseInterval;
                wasDown = false;
            }
            else
            {
                failCount++;
                wasDown = true;
                Log.Warning("Page unreachable (HTTP {Code}) — failure {Fail}", httpCode, failCount);

                if (failCount >= config.MaxFailBeforeRestart)
                {
                    if (restartCount < config.MaxRestarts)
                    {
                        restartCount++;
                        Log.Warning("Restarting Chromium (attempt {Count}/{Max})", restartCount, config.MaxRestarts);
                        chromium.Launch();
                        failCount = 0;
                        currentInterval = Math.Min(currentInterval * 2, maxInterval);
                        Log.Information("Next check in {Interval}s", currentInterval);
                    }
                    else
                    {
                        currentInterval = maxInterval;
                        Log.Warning("Site appears down. Polling at {Interval}s for recovery", currentInterval);
                    }
                }
                else
                {
                    chromium.Refresh();
                }
            }
        }
    }

    private async Task<DateTimeOffset?> MaybeCheckForUpdatesAsync(CancellationToken ct, DateTimeOffset lastCheck)
    {
        if (DateTimeOffset.UtcNow - lastCheck < TimeSpan.FromMinutes(config.UpdateCheckIntervalMinutes))
        {
            return null;
        }

        await updater.CheckAndApplyAsync(ct);
        return DateTimeOffset.UtcNow;
    }

    private static async Task<int> HealthCheckAsync(HttpClient http, string url, CancellationToken ct)
    {
        try
        {
            using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            return (int)response.StatusCode;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Health check request failed for {Url}", url);
            return 0;
        }
    }

    private static void PrepareDisplay()
    {
        Shell.Run("xset", "s", "off");
        Shell.Run("xset", "-dpms");
        Shell.Run("xset", "s", "noblank");
        Shell.Start("unclutter", "-idle", "0.5", "-root");
    }
}
