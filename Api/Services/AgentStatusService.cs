using Api.Data.Entities;
using Api.Interfaces;
using EfCoreRepository.Extensions;
using EfCoreRepository.Interfaces;
using Shared.Contracts;

namespace Api.Services;

public sealed class AgentStatusService(IEfRepository repository) : IAgentStatusService
{
    private IBasicCrud<KioskAgentInstance> Dal => repository.For<KioskAgentInstance>();

    private static AgentStatusDto ToDto(KioskAgentInstance a) =>
        new(a.Id, a.MachineName, a.AgentVersion, a.DisplayUrl, a.ChromiumRunning, a.IpAddress, a.FirstSeenAt, a.LastSeenAt);

    public async Task RecordHeartbeatAsync(AgentHeartbeatRequest req, string? ip)
    {
        var machineName = req.MachineName.Trim();
        var now = DateTimeOffset.UtcNow;

        var existing = (await Dal.GetAll(
            filterExprs: [a => a.MachineName == machineName],
            maxResults: 1)).FirstOrDefault();

        if (existing is null)
        {
            await Dal.Save(new KioskAgentInstance
            {
                MachineName = machineName,
                AgentVersion = req.AgentVersion,
                DisplayUrl = req.DisplayUrl,
                ChromiumRunning = req.ChromiumRunning,
                IpAddress = ip,
                FirstSeenAt = now,
                LastSeenAt = now
            });
            return;
        }

        await Dal.Update(existing.Id, a =>
        {
            a.AgentVersion = req.AgentVersion;
            a.DisplayUrl = req.DisplayUrl;
            a.ChromiumRunning = req.ChromiumRunning;
            a.IpAddress = ip;
            a.LastSeenAt = now;
        });
    }

    public async Task<List<AgentStatusDto>> GetAllAsync()
    {
        var items = (await Dal.GetAll(project: a => ToDto(a))).ToList();
        return items.OrderByDescending(a => a.LastSeenAt).ToList();
    }
}
