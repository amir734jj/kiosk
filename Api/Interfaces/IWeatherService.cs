using Shared.Contracts;

namespace Api.Interfaces;

public interface IWeatherService
{
    Task<WeatherDto?> GetWeatherAsync(string? address);
}
