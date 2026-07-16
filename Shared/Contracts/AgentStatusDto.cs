namespace Shared.Contracts;

public record AgentStatusDto(
    int Id,
    string MachineName,
    string AgentVersion,
    string DisplayUrl,
    bool ChromiumRunning,
    string? IpAddress,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastSeenAt);
