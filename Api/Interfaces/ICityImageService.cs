using Shared.Contracts;

namespace Api.Interfaces;

public interface ICityImageService
{
    Task<string?> GetCityImageUrlAsync(string? city);
}
