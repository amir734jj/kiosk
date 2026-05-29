using Api.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;

namespace Api.Controllers;

[ApiController]
[Route("api/public")]
public class PublicImageController(
    ICityImageService cityImageService,
    ISpaceStorage backgroundImageStore,
    IGlobalConfigService configService,
    IHttpClientFactory httpClientFactory,
    ILogger<PublicImageController> logger) : ControllerBase
{
    [HttpGet("background")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> GetBackgroundImage()
    {
        var config = await configService.GetAsync();

        try
        {
            return config.BackgroundStyle switch
            {
                BackgroundStyle.CityPhoto => await GetCityPhoto(config.City),
                BackgroundStyle.StaticPhoto => await GetStaticPhoto(),
                BackgroundStyle.UploadedImage => await GetUploadedImage(),
                _ => NotFound()
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Background image failed for style {Style}, falling back", config.BackgroundStyle);
            return NotFound();
        }
    }

    private async Task<IActionResult> GetCityPhoto(string? city)
    {
        if (string.IsNullOrWhiteSpace(city))
            return NotFound();

        var imageUrl = await cityImageService.GetCityImageUrlAsync(city);
        if (string.IsNullOrEmpty(imageUrl))
            return NotFound();

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("KioskApp/1.0");
        var resp = await client.GetAsync(imageUrl);
        if (!resp.IsSuccessStatusCode)
            return NotFound();

        var contentType = resp.Content.Headers.ContentType?.ToString() ?? "image/jpeg";
        var bytes = await resp.Content.ReadAsByteArrayAsync();
        return File(bytes, contentType);
    }

    private async Task<IActionResult> GetStaticPhoto()
    {
        var result = await cityImageService.GetStaticImageAsync();
        return File(result.Data, result.ContentType);
    }

    private async Task<IActionResult> GetUploadedImage()
    {
        var result = await backgroundImageStore.GetRandomAsync();
        if (result is null)
            return NotFound();

        return File(result.Value.Data, result.Value.ContentType);
    }
}
