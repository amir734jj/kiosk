namespace Shared.Contracts;

public record AgentConfigDto(
    string DisplayUrl,
    string? BetterStackSourceToken,
    string? BetterStackIngestingHost,
    string? UpdateFeedUrl,
    int HealthCheckIntervalSeconds,
    int MaxFailBeforeRestart,
    int MaxRestarts,
    int MaxIntervalSeconds,
    int UpdateCheckIntervalMinutes);
