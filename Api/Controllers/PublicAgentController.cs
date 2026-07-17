using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;
using Api.Interfaces;

namespace Api.Controllers;

// Anonymous: the Better Stack source token is a write-only ingest token, not a secret.
[ApiController]
[Route("api/public/agent-config")]
public class PublicAgentController(IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    [ResponseCache(Duration = 60)]
    public ActionResult<AgentConfigDto> GetAgentConfig()
    {
        var dto = new AgentConfigDto(
            DisplayUrl: configuration["Agent:DisplayUrl"] ?? "https://kiosk.coolify.hesamian.com/display",
            BetterStackSourceToken: configuration["BetterStack:SourceToken"],
            BetterStackIngestingHost: configuration["BetterStack:IngestingHost"],
            UpdateFeedUrl: configuration["Agent:UpdateFeedUrl"],
            HealthCheckIntervalSeconds: configuration.GetValue("Agent:HealthCheckIntervalSeconds", 120),
            MaxFailBeforeRestart: configuration.GetValue("Agent:MaxFailBeforeRestart", 3),
            MaxRestarts: configuration.GetValue("Agent:MaxRestarts", 3),
            MaxIntervalSeconds: configuration.GetValue("Agent:MaxIntervalSeconds", 600),
            UpdateCheckIntervalMinutes: configuration.GetValue("Agent:UpdateCheckIntervalMinutes", 60));

        return Ok(dto);
    }

    [HttpPost("/api/public/agent-heartbeat")]
    public async Task<IActionResult> Heartbeat(
        [FromBody] AgentHeartbeatRequest request,
        [FromServices] IAgentStatusService agentStatusService)
    {
        if (string.IsNullOrWhiteSpace(request.MachineName))
        {
            return BadRequest("MachineName is required.");
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await agentStatusService.RecordHeartbeatAsync(request, ip);
        return NoContent();
    }
}
