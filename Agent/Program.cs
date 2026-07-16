using System.Reflection;
using Kiosk.Agent;
using Serilog;
using Shared.Contracts;
using Velopack;

VelopackApp.Build().Run();

var settings = AgentSettings.Load(args);

Log.Logger = LoggingSetup.CreateBootstrapLogger();

var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";

AgentConfigDto config;
try
{
    Log.Information("Kiosk agent {Version} starting on {Machine}. Backend {Backend}",
        version, Environment.MachineName, settings.BackendUrl);
    config = await AgentConfigClient.FetchWithRetryAsync(settings.BackendUrl);
    Log.Information("Fetched agent config. Display {Url}", config.DisplayUrl);
}
catch (Exception ex)
{
    Log.Warning(ex, "Could not reach backend for config — falling back to local defaults");
    config = settings.Fallback();
}

Log.CloseAndFlush();
Log.Logger = LoggingSetup.CreateLogger(config);

var betterStackEnabled = !string.IsNullOrWhiteSpace(config.BetterStackSourceToken)
    && !string.IsNullOrWhiteSpace(config.BetterStackIngestingHost);
Log.Information("Kiosk agent {Version} online on {Machine}. Better Stack shipping {State}",
    version, Environment.MachineName, betterStackEnabled ? "enabled" : "disabled");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Log.Information("Shutdown requested");
    cts.Cancel();
};
AppDomain.CurrentDomain.ProcessExit += (_, _) => cts.Cancel();

var chromium = new ChromiumLauncher(config.DisplayUrl);
var updater = new UpdateService(config.UpdateFeedUrl);
var heartbeat = new HeartbeatReporter(settings.BackendUrl, version, config.DisplayUrl);
var watchdog = new KioskWatchdog(config, chromium, updater, heartbeat);

try
{
    await watchdog.RunAsync(cts.Token);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Kiosk agent crashed");
}
finally
{
    chromium.Kill();
    Log.Information("Kiosk agent stopped");
    await Log.CloseAndFlushAsync();
}
