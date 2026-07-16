using Shared.Contracts;

namespace Api.Interfaces;

public interface IAgentStatusService
{
    Task RecordHeartbeatAsync(AgentHeartbeatRequest req, string? ip);
    Task<List<AgentStatusDto>> GetAllAsync();
}
