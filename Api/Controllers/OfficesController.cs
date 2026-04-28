using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Api.Data;
using Api.Data.Entities;
using Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.Contracts;

namespace Api.Controllers;

[ApiController]
[Route("api/offices")]
[Authorize]
public class OfficesController(IOfficeService officeService, UserManager<AppUser> users) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await officeService.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var office = await officeService.GetByIdAsync(id);
        return office is null ? NotFound() : Ok(office);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create(CreateOfficeRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.UnitNumber))
        {
            return BadRequest("Unit number is required.");
        }

        if (string.IsNullOrWhiteSpace(req.Name))
        {
            return BadRequest("Office name is required.");
        }

        if (req.Names is null || req.Names.Count == 0)
        {
            return BadRequest("At least one name is required.");
        }

        if (await officeService.ExistsByUnitAsync(req.UnitNumber))
        {
            return Conflict($"Unit '{req.UnitNumber}' already exists.");
        }

        var office = await officeService.CreateAsync(req);
        return Ok(office);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(int id, UpdateOfficeRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.UnitNumber))
        {
            return BadRequest("Unit number is required.");
        }

        if (string.IsNullOrWhiteSpace(req.Name))
        {
            return BadRequest("Office name is required.");
        }

        if (req.Names is null || req.Names.Count == 0)
        {
            return BadRequest("At least one name is required.");
        }

        if (await officeService.ExistsByUnitAsync(req.UnitNumber, id))
        {
            return Conflict($"Unit '{req.UnitNumber}' already exists.");
        }

        var updated = await officeService.UpdateAsync(id, req);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await officeService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyOffice()
    {
        var user = await GetCurrentUser();
        if (user?.OfficeId is null)
        {
            return NotFound("No office assigned.");
        }

        var office = await officeService.GetByIdAsync(user.OfficeId.Value);
        return office is null ? NotFound() : Ok(office);
    }

    [HttpPut("my")]
    public async Task<IActionResult> UpdateMyOffice(UpdateOfficeRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
        {
            return BadRequest("Office name is required.");
        }

        if (req.Names is null || req.Names.Count == 0)
        {
            return BadRequest("At least one name is required.");
        }

        var user = await GetCurrentUser();
        if (user?.OfficeId is null)
        {
            return NotFound("No office assigned.");
        }

        var updated = await officeService.UpdateMyAsync(user.OfficeId.Value, req);
        return updated ? NoContent() : NotFound();
    }

    [HttpPost("{officeId:int}/assign-user/{userId:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> AssignUser(int officeId, int userId)
    {
        var office = await officeService.GetByIdAsync(officeId);
        if (office is null)
        {
            return NotFound("Office not found.");
        }

        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return NotFound("User not found.");
        }

        user.OfficeId = officeId;
        await users.UpdateAsync(user);
        return NoContent();
    }

    [HttpPost("unassign-user/{userId:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UnassignUser(int userId)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return NotFound("User not found.");
        }

        user.OfficeId = null;
        await users.UpdateAsync(user);
        return NoContent();
    }

    private async Task<AppUser?> GetCurrentUser()
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId is null ? null : await users.FindByIdAsync(userId);
    }
}
