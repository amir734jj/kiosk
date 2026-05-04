using Api.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;

namespace Api.Controllers;

[ApiController]
[Route("api/public")]
public class PublicController(IOfficeService officeService, IAnnouncementService announcementService, IWeatherService weatherService, IGlobalConfigService configService, ICityImageService cityImageService, IHolidayService holidayService) : ControllerBase
{
    [HttpGet("display")]
    [ResponseCache(NoStore = true)]
    public async Task<IActionResult> GetDisplay()
    {
        var (offices, announcements, config, todayHoliday) = await (
            officeService.GetAllAsync(),
            announcementService.GetActiveAsync(),
            configService.GetAsync(),
            holidayService.GetTodayHolidayAsync()
        );

        var hasCity = !string.IsNullOrWhiteSpace(config.City);

        var (weather, imageUrl) = await (
            hasCity ? weatherService.GetWeatherAsync(config.City) : Task.FromResult<WeatherDto?>(null),
            hasCity && config.ShowCityImage
                ? cityImageService.GetCityImageUrlAsync(config.City)
                : Task.FromResult<string?>(null)
        );

        var hasBackgroundImage = imageUrl is not null;

        var publicOffices = offices
            .Select(o => new PublicOfficeDto(o.UnitNumber, o.Name, o.Names, o.PhoneNumber, o.Note))
            .ToList();

        var kioskName = !string.IsNullOrWhiteSpace(config.KioskName) ? config.KioskName : null;

        return Ok(new PublicDisplayDto(publicOffices, announcements, weather, hasBackgroundImage, todayHoliday, kioskName, DateTimeOffset.UtcNow));
    }
}
