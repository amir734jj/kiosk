using Api.Extensions;
using Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.Contracts;

namespace Api.Controllers;

[ApiController]
[Route("api/global-config")]
[Authorize(Roles = Roles.Admin)]
public sealed class GlobalConfigController(IGlobalConfigService configService, ISpaceStorage backgroundImageStore) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var config = await configService.GetAsync();
        return Ok(config);
    }

    [HttpPut]
    public async Task<IActionResult> Save([FromBody] GlobalConfigModel config)
    {
        await configService.SaveAsync(config);
        return NoContent();
    }

    [HttpPost("background-image")]
    public async Task<IActionResult> UploadBackgroundImage(IFormFile file)
    {
        if (file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        if (file.Length > ImageUpload.MaxFileSize)
        {
            return BadRequest("File must be under 10 MB.");
        }

        if (!ImageUpload.AllowedContentTypes.Contains(file.ContentType))
        {
            return BadRequest("Only JPEG, PNG, and WebP images are allowed.");
        }

        var username = User.TryGetUsername() ?? "unknown";

        await using var stream = file.OpenReadStream();
        await backgroundImageStore.UploadAsync(stream, file.ContentType, file.FileName, username);

        return Ok(new { message = "Background image uploaded." });
    }

    [HttpGet("background-images")]
    public async Task<IActionResult> ListBackgroundImages()
    {
        var images = await backgroundImageStore.ListAsync();
        return Ok(images);
    }

    [HttpDelete("background-image/{id}")]
    public async Task<IActionResult> DeleteBackgroundImage(string id)
    {
        await backgroundImageStore.DeleteAsync(id);
        return NoContent();
    }
}
