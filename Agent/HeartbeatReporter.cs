using Refit;
using Serilog;
using Shared.Contracts;
using Shared.Contracts.Interfaces;

namespace Kiosk.Agent;

public sealed class HeartbeatReporter
{
    private readonly IPublicApi _api;
    private readonly string _version;
    private readonly string _displayUrl;

    public HeartbeatReporter(string backendUrl, string version, string displayUrl)
    {
        _api = RestService.For<IPublicApi>(backendUrl);
        _version = version;
        _displayUrl = displayUrl;
    }

    public async Task SendAsync(bool chromiumRunning, CancellationToken ct)
    {
        try
        {
            await _api.PostAgentHeartbeatAsync(new AgentHeartbeatRequest(
                Environment.MachineName, _version, _displayUrl, chromiumRunning));
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Heartbeat failed");
        }
    }
}
