using Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/public/ads")]
public class PublicAdvertisementController(IAdvertisementService advertisementService) : ControllerBase
{
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveAds()
    {
        var ads = await advertisementService.GetActiveAsync();
        return Ok(ads);
    }

    [HttpGet("{adId:int}/photos/{photoId:guid}")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> GetPhoto(int adId, string photoId)
    {
        var result = await advertisementService.GetPhotoAsync(adId, photoId);
        if (result is null) return NotFound();

        return File(result.Value.Data, result.Value.ContentType, result.Value.OriginalFileName);
    }
}
