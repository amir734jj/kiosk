using Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/public")]
public class PublicImageController(
    ICityImageService cityImageService,
    IGlobalConfigService configService,
    IHttpClientFactory httpClientFactory,
    ILogger<PublicImageController> logger) : ControllerBase
{
    [HttpGet("background")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> GetBackgroundImage()
    {
        var config = await configService.GetAsync();
        if (!config.ShowCityImage || string.IsNullOrWhiteSpace(config.City))
        {
            return NotFound();
        }

        var imageUrl = await cityImageService.GetCityImageUrlAsync(config.City);
        if (string.IsNullOrEmpty(imageUrl))
        {
            return NotFound();
        }

        try
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("KioskApp/1.0");
            var resp = await client.GetAsync(imageUrl);
            if (!resp.IsSuccessStatusCode)
            {
                return NotFound();
            }

            var contentType = resp.Content.Headers.ContentType?.ToString() ?? "image/jpeg";
            var bytes = await resp.Content.ReadAsByteArrayAsync();
            return File(bytes, contentType);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to proxy background image");
            return NotFound();
        }
    }
}
