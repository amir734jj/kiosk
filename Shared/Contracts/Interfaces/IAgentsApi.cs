using Refit;

namespace Shared.Contracts.Interfaces;

[Headers("Authorization: Bearer")]
public interface IAgentsApi
{
    [Get("/api/agents")]
    Task<List<AgentStatusDto>> GetAllAsync();
}
