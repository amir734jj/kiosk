using Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace Api.Controllers;

[ApiController]
[Route("api/agents")]
[Authorize(Roles = Roles.Admin)]
public class AgentsController(IAgentStatusService agentStatusService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await agentStatusService.GetAllAsync());
    }
}
