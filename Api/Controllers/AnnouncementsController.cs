using Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.Contracts;

namespace Api.Controllers;

[ApiController]
[Route("api/announcements")]
[Authorize(Roles = Roles.Admin)]
public class AnnouncementsController(IAnnouncementService announcementService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await announcementService.GetAllAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAnnouncementRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
        {
            return BadRequest("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(req.Content))
        {
            return BadRequest("Content is required.");
        }

        var announcement = await announcementService.CreateAsync(req);
        return Ok(announcement);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateAnnouncementRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
        {
            return BadRequest("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(req.Content))
        {
            return BadRequest("Content is required.");
        }

        var updated = await announcementService.UpdateAsync(id, req);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await announcementService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
