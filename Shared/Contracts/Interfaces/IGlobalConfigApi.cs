using Refit;

namespace Shared.Contracts.Interfaces;

[Headers("Authorization: Bearer")]
public interface IGlobalConfigApi
{
    [Get("/api/global-config")]
    Task<GlobalConfigModel> GetAsync();

    [Put("/api/global-config")]
    Task SaveAsync([Body] GlobalConfigModel config);

    [Get("/api/global-config/background-images")]
    Task<List<BackgroundImageDto>> ListBackgroundImagesAsync();

    [Delete("/api/global-config/background-image/{id}")]
    Task DeleteBackgroundImageAsync(string id);
}
