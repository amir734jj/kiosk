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
        var offices = await officeService.GetAllAsync();
        var announcements = await announcementService.GetActiveAsync();

        var config = await configService.GetAsync();
        var hasCity = !string.IsNullOrWhiteSpace(config.City);

        var weather = hasCity ? await weatherService.GetWeatherAsync(config.City) : null;
        var backgroundImageUrl = hasCity && config.ShowCityImage ? await cityImageService.GetCityImageUrlAsync(config.City) : null;
        var todayHoliday = await holidayService.GetTodayHolidayAsync();

        var publicOffices = offices
            .Select(o => new PublicOfficeDto(o.UnitNumber, o.Name, o.Names, o.PhoneNumber, o.Note))
            .ToList();

        var kioskName = !string.IsNullOrWhiteSpace(config.KioskName) ? config.KioskName : null;

        return Ok(new PublicDisplayDto(publicOffices, announcements, weather, backgroundImageUrl, todayHoliday, kioskName, DateTimeOffset.UtcNow));
    }
}
