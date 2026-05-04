using Api.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shared.Contracts;

namespace Api.Pages;

public class DisplayModel(IOfficeService officeService, IAnnouncementService announcementService, IWeatherService weatherService, IGlobalConfigService configService, ICityImageService cityImageService, IHolidayService holidayService) : PageModel
{
    public PublicDisplayDto? Data { get; private set; }

    public async Task OnGetAsync()
    {
        // DB calls — sequential (DbContext is not thread-safe)
        var offices = await officeService.GetAllAsync();
        var announcements = await announcementService.GetActiveAsync();
        var config = await configService.GetAsync();

        var hasCity = !string.IsNullOrWhiteSpace(config.City);

        // External API calls — parallel
        var (weather, imageUrl, todayHoliday) = await (
            hasCity ? weatherService.GetWeatherAsync(config.City) : Task.FromResult<WeatherDto?>(null),
            hasCity && config.ShowCityImage
                ? cityImageService.GetCityImageUrlAsync(config.City)
                : Task.FromResult<string?>(null),
            holidayService.GetTodayHolidayAsync()
        );

        var hasBackgroundImage = imageUrl is not null;

        var kioskName = !string.IsNullOrWhiteSpace(config.KioskName) ? config.KioskName : null;

        var publicOffices = offices
            .Select(o => new PublicOfficeDto(o.UnitNumber, o.Name, o.Names, o.PhoneNumber, o.Note))
            .ToList();

        Data = new PublicDisplayDto(publicOffices, announcements, weather, hasBackgroundImage, todayHoliday, kioskName, DateTimeOffset.UtcNow);
    }
}
