using Api.Interfaces;
using Newtonsoft.Json.Linq;

namespace Api.Services;

public class HolidayService(IHttpClientFactory httpClientFactory, ILogger<HolidayService> logger) : IHolidayService
{
    public async Task<string?> GetTodayHolidayAsync()
    {
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
                return json["public_holiday_name"]?.ToString();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch holiday info");
        }

        return null;
    }
}
