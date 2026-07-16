using Refit;

namespace Shared.Contracts.Interfaces;

public interface IPublicApi
{
    [Get("/api/public/display")]
    Task<PublicDisplayDto> GetDisplayAsync();

    [Get("/api/public/agent-config")]
    Task<AgentConfigDto> GetAgentConfigAsync();

    [Post("/api/public/agent-heartbeat")]
    Task PostAgentHeartbeatAsync([Body] AgentHeartbeatRequest request);
}
