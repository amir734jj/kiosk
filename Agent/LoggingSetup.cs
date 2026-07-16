using Serilog;
using Serilog.Core;
using Serilog.Formatting.Compact;
using Shared.Contracts;

namespace Kiosk.Agent;

public static class LoggingSetup
{
    private const string ConsoleTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

    public static Logger CreateBootstrapLogger() => Base().CreateLogger();

    public static Logger CreateLogger(AgentConfigDto config)
    {
        var cfg = Base();

        if (!string.IsNullOrWhiteSpace(config.BetterStackSourceToken) &&
            !string.IsNullOrWhiteSpace(config.BetterStackIngestingHost))
        {
            cfg.WriteTo.BetterStack(
                sourceToken: config.BetterStackSourceToken,
                betterStackEndpoint: $"https://{config.BetterStackIngestingHost}");
        }

        return cfg.CreateLogger();
    }

    private static LoggerConfiguration Base() => new LoggerConfiguration()
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "kiosk-agent")
        .Enrich.WithProperty("Machine", Environment.MachineName)
        .WriteTo.Console(outputTemplate: ConsoleTemplate)
        .WriteTo.File(
            new CompactJsonFormatter(),
            path: "logs/agent-.json",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7);
}
