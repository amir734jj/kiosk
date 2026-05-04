using Api.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;

namespace Api.Services;

public class HolidayService(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<HolidayService> logger) : IHolidayService
{
    public async Task<string?> GetTodayHolidayAsync()
    {
        var cacheKey = $"holiday:{DateTime.Today:yyyy-MM-dd}";
        if (cache.TryGetValue(cacheKey, out string? cached))
        {
            return cached;
        }

        try
        {
            var client = httpClientFactory.CreateClient();

            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var url = $"https://api.api-ninjas.com/v1/ispublicholiday?country=US&date={today}";

            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }

            var json = JObject.Parse(await resp.Content.ReadAsStringAsync());

            if (json["is_public_holiday"]?.Value<bool>() == true)
            {
                var name = json["public_holiday_name"]?.ToString();
                cache.Set(cacheKey, name, TimeSpan.FromHours(6));
                return name;
            }

            cache.Set(cacheKey, (string?)null, TimeSpan.FromHours(6));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch holiday info");
        }

        return null;
    }
}
