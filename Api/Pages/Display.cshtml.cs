using Api.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shared.Contracts;

namespace Api.Pages;

public class DisplayModel(IOfficeService officeService, IAnnouncementService announcementService, IWeatherService weatherService, IGlobalConfigService configService, ICityImageService cityImageService, IHolidayService holidayService) : PageModel
{
    public PublicDisplayDto? Data { get; private set; }

    public async Task OnGetAsync()
    {
        var offices = await officeService.GetAllAsync();
        var announcements = await announcementService.GetActiveAsync();

        var config = await configService.GetAsync();
        var hasCity = !string.IsNullOrWhiteSpace(config.City);

        var weather = hasCity ? await weatherService.GetWeatherAsync(config.City) : null;
        var backgroundImageUrl = hasCity && config.ShowCityImage ? await cityImageService.GetCityImageUrlAsync(config.City) : null;
        var todayHoliday = await holidayService.GetTodayHolidayAsync();

        var kioskName = !string.IsNullOrWhiteSpace(config.KioskName) ? config.KioskName : null;

        var publicOffices = offices
            .Select(o => new PublicOfficeDto(o.UnitNumber, o.Name, o.Names, o.PhoneNumber, o.Note))
            .ToList();

        Data = new PublicDisplayDto(publicOffices, announcements, weather, backgroundImageUrl, todayHoliday, kioskName, DateTimeOffset.UtcNow);
    }
}
