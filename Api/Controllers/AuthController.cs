using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Api.Data;
using Api.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Shared;
using Shared.Contracts;

namespace Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<AppUser> users,
    RoleManager<AppRole> roles,
    IConfiguration config) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || req.Username.Length < 3)
        {
            return BadRequest("Username must be at least 3 characters.");
        }

        if (req.Password != req.PasswordConfirm)
        {
            return BadRequest("Passwords do not match.");
        }

        foreach (var role in new[] { Roles.Admin, Roles.User })
        {
            if (!await roles.RoleExistsAsync(role))
            {
                await roles.CreateAsync(new AppRole { Name = role });
            }
        }

        var isFirstUser = !users.Users.Any();
        var assignedRole = isFirstUser ? Roles.Admin : Roles.User;

        var user = new AppUser
        {
            UserName = req.Username,
            IsActive = isFirstUser
        };

        var result = await users.CreateAsync(user, req.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        await users.AddToRoleAsync(user, assignedRole);
        return Ok(new RegisterResponse(isFirstUser));
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var user = await users.FindByNameAsync(req.Username);
        if (user is null || !await users.CheckPasswordAsync(user, req.Password))
        {
            return Unauthorized("Invalid username or password.");
        }

        if (!user.IsActive)
        {
            return StatusCode(403, "Account is pending activation by an administrator.");
        }

        var userRoles = await users.GetRolesAsync(user);
        var role = userRoles.FirstOrDefault() ?? Roles.User;

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await users.UpdateAsync(user);

        return Ok(new LoginResponse(BuildToken(user, role), role, user.Id));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        var user = await users.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        var userRoles = await users.GetRolesAsync(user);
        return Ok(new MeResponse(user.UserName!, userRoles.FirstOrDefault() ?? Roles.User, user.OfficeId));
    }

    [HttpPost("impersonate/{userId:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Impersonate(int userId)
    {
        var targetUser = await users.FindByIdAsync(userId.ToString());
        if (targetUser is null)
        {
            return NotFound();
        }

        var userRoles = await users.GetRolesAsync(targetUser);
        var role = userRoles.FirstOrDefault() ?? Roles.User;

        return Ok(new LoginResponse(BuildToken(targetUser, role), role, targetUser.Id));
    }

    private string BuildToken(AppUser user, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName!),
                new Claim(ClaimTypes.Role, role)
            ],
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
