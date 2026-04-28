using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Api.Data;
using Api.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Contracts;

namespace Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = Roles.Admin)]
public class UsersController(UserManager<AppUser> users) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userList = await users.Users
            .Include(u => u.Office)
            .OrderBy(u => u.UserName)
            .ToListAsync();

        var result = new List<UserDto>();
        foreach (var u in userList)
        {
            var userRoles = await users.GetRolesAsync(u);
            result.Add(new UserDto(
                u.Id,
                u.UserName!,
                userRoles.FirstOrDefault() ?? Roles.User,
                u.IsActive,
                u.OfficeId,
                u.Office?.Name,
                u.LastLoginAt));
        }
        return Ok(result);
    }

    [HttpPost("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id)
    {
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        user.IsActive = true;
        await users.UpdateAsync(user);
        return NoContent();
    }

    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        user.IsActive = false;
        await users.UpdateAsync(user);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        await users.DeleteAsync(user);
        return NoContent();
    }

    [HttpPost("{id:int}/make-admin")]
    public async Task<IActionResult> MakeAdmin(int id)
    {
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        await users.RemoveFromRoleAsync(user, Roles.User);
        await users.AddToRoleAsync(user, Roles.Admin);
        return NoContent();
    }

    [HttpPost("{id:int}/make-user")]
    public async Task<IActionResult> MakeUser(int id)
    {
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        await users.RemoveFromRoleAsync(user, Roles.Admin);
        await users.AddToRoleAsync(user, Roles.User);
        return NoContent();
    }
}
