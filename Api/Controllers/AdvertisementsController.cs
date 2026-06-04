using Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.Contracts;

namespace Api.Controllers;

[ApiController]
[Route("api/advertisements")]
[Authorize(Roles = Roles.Admin)]
public class AdvertisementsController(IAdvertisementService advertisementService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await advertisementService.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ad = await advertisementService.GetByIdAsync(id);
        return ad is not null ? Ok(ad) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAdvertisementRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest("Title is required.");

        if (string.IsNullOrWhiteSpace(req.Description))
            return BadRequest("Description is required.");

        var ad = await advertisementService.CreateAsync(req);
        return Ok(ad);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateAdvertisementRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest("Title is required.");

        if (string.IsNullOrWhiteSpace(req.Description))
            return BadRequest("Description is required.");

        var updated = await advertisementService.UpdateAsync(id, req);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await advertisementService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id:int}/photos")]
    public async Task<IActionResult> UploadPhoto(int id, IFormFile file)
    {
        var ad = await advertisementService.GetByIdAsync(id);
        if (ad is null) return NotFound();

        if (file.Length == 0)
            return BadRequest("File is empty.");

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest("File size exceeds 10 MB limit.");

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest("Only JPEG, PNG, GIF, and WebP images are allowed.");

        await using var stream = file.OpenReadStream();
        var photo = await advertisementService.UploadPhotoAsync(id, stream, file.ContentType, file.FileName);
        return Ok(photo);
    }

    [HttpGet("{id:int}/photos")]
    public async Task<IActionResult> GetPhotos(int id)
    {
        var ad = await advertisementService.GetByIdAsync(id);
        if (ad is null) return NotFound();

        var photos = await advertisementService.GetPhotosAsync(id);
        return Ok(photos);
    }

    [HttpDelete("{id:int}/photos/{photoId}")]
    public async Task<IActionResult> DeletePhoto(int id, string photoId)
    {
        var deleted = await advertisementService.DeletePhotoAsync(id, photoId);
        return deleted ? NoContent() : NotFound();
    }
}
