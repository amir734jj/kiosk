using Api.Interfaces;
using Newtonsoft.Json.Linq;

namespace Api.Services;

public class CityImageService(IHttpClientFactory httpClientFactory, ILogger<CityImageService> logger) : ICityImageService
{
    public async Task<string?> GetCityImageUrlAsync(string? city)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return null;
        }

        // 1. Wikimedia Commons: multiple city photos, rotate daily
        try
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("KioskApp/1.0 (office-directory)");
            var encoded = Uri.EscapeDataString($"{city.Trim()} skyline city");

            var url = $"https://commons.wikimedia.org/w/api.php?action=query&generator=search&gsrsearch={encoded}&gsrlimit=10&gsrnamespace=6&prop=imageinfo&iiprop=url&iiurlwidth=1920&format=json";
            var resp = await client.GetAsync(url);
            if (resp.IsSuccessStatusCode)
            {
                var json = JObject.Parse(await resp.Content.ReadAsStringAsync());
                var pages = json["query"]?["pages"];
                if (pages != null)
                {
                    var imageUrls = pages.Children<JProperty>()
                        .Select(p => p.Value["imageinfo"]?[0]?["thumburl"]?.ToString())
                        .Where(u => !string.IsNullOrEmpty(u))
                        .ToList();

                    if (imageUrls.Count > 0)
                    {
                        var dayIndex = DateTime.UtcNow.Hour % imageUrls.Count;
                        return imageUrls[dayIndex];
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch Wikimedia Commons images for: {City}", city);
        }

        // 2. Bing Image of the Day (changes daily, not city-specific)
        try
        {
            var client = httpClientFactory.CreateClient();
            var resp = await client.GetAsync("https://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1&mkt=en-US");
            if (resp.IsSuccessStatusCode)
            {
                var json = JObject.Parse(await resp.Content.ReadAsStringAsync());
                var urlBase = json["images"]?[0]?["url"]?.ToString();
                if (!string.IsNullOrEmpty(urlBase))
                {
                    return $"https://www.bing.com{urlBase}";
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch Bing image of the day");
        }

        // 3. Wikipedia: city's main image (static fallback)
        try
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("KioskApp/1.0 (office-directory)");
            var encoded = Uri.EscapeDataString(city.Trim());

            var resp = await client.GetAsync($"https://en.wikipedia.org/api/rest_v1/page/summary/{encoded}");
            if (resp.IsSuccessStatusCode)
            {
                var json = JObject.Parse(await resp.Content.ReadAsStringAsync());

                var originalSource = json["originalimage"]?["source"]?.ToString();
                if (!string.IsNullOrEmpty(originalSource))
                {
                    return originalSource;
                }

                var thumbSource = json["thumbnail"]?["source"]?.ToString();
                if (!string.IsNullOrEmpty(thumbSource))
                {
                    return thumbSource;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch Wikipedia image for: {City}", city);
        }

        return null;
    }
}
