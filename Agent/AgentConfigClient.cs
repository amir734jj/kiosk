using Refit;
using Serilog;
using Shared.Contracts;
using Shared.Contracts.Interfaces;

namespace Kiosk.Agent;

public static class AgentConfigClient
{
    private const int MaxAttempts = 10;

    public static async Task<AgentConfigDto> FetchWithRetryAsync(string backendUrl, CancellationToken ct = default)
    {
        var api = RestService.For<IPublicApi>(backendUrl);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await api.GetAgentConfigAsync();
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                var delay = TimeSpan.FromSeconds(Math.Min(30, attempt * 5));
                Log.Warning("Config fetch attempt {Attempt}/{Max} failed: {Message}. Retrying in {Delay}s",
                    attempt, MaxAttempts, ex.Message, delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }
        }
    }
}
