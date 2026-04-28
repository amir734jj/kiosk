using Api.Interfaces;
using OpenMeteo;
using Shared.Contracts;

namespace Api.Services;

public class WeatherService(ILogger<WeatherService> logger) : IWeatherService
{
    private readonly OpenMeteoClient _client = new();

    public async Task<WeatherDto?> GetWeatherAsync(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        try
        {
            var options = new WeatherForecastOptions
            {
                Temperature_Unit = TemperatureUnitType.fahrenheit,
                Current = CurrentOptions.All
            };
            var forecast = await _client.QueryAsync(address, options);

            if (forecast?.Current == null)
            {
                logger.LogWarning("Weather forecast returned null Current for: {Address}", address);
                return null;
            }

            var tempF = (double)(forecast.Current.Temperature ?? 0);
            var weatherCode = forecast.Current.Weathercode ?? 0;

            var (description, icon) = MapWeatherCode(weatherCode);
            return new WeatherDto(tempF, description, icon);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch weather for address: {Address}", address);
            return null;
        }
    }

    private static (string Description, string Icon) MapWeatherCode(int code) => code switch
    {
        0 => ("Clear sky", "bi-sun-fill"),
        1 => ("Mainly clear", "bi-sun-fill"),
        2 => ("Partly cloudy", "bi-cloud-sun-fill"),
        3 => ("Overcast", "bi-clouds-fill"),
        45 or 48 => ("Foggy", "bi-cloud-fog-fill"),
        51 or 53 or 55 => ("Drizzle", "bi-cloud-drizzle-fill"),
        61 or 63 or 65 => ("Rain", "bi-cloud-rain-fill"),
        66 or 67 => ("Freezing rain", "bi-cloud-sleet-fill"),
        71 or 73 or 75 => ("Snow", "bi-cloud-snow-fill"),
        77 => ("Snow grains", "bi-cloud-snow-fill"),
        80 or 81 or 82 => ("Rain showers", "bi-cloud-rain-heavy-fill"),
        85 or 86 => ("Snow showers", "bi-cloud-snow-fill"),
        95 => ("Thunderstorm", "bi-cloud-lightning-fill"),
        96 or 99 => ("Thunderstorm with hail", "bi-cloud-lightning-rain-fill"),
        _ => ("Unknown", "bi-question-circle")
    };
}
