namespace Shared.Contracts;

public record AgentHeartbeatRequest(
    string MachineName,
    string AgentVersion,
    string DisplayUrl,
    bool ChromiumRunning);
