using Refit;

namespace Shared.Contracts.Interfaces;

[Headers("Authorization: Bearer")]
public interface IGlobalConfigApi
{
    [Get("/api/global-config")]
    Task<GlobalConfigModel> GetAsync();

    [Put("/api/global-config")]
    Task SaveAsync([Body] GlobalConfigModel config);

    [Multipart]
    [Post("/api/global-config/background-image")]
    Task UploadBackgroundImageAsync([AliasAs("file")] StreamPart file);

    [Get("/api/global-config/background-images")]
    Task<List<BackgroundImageDto>> ListBackgroundImagesAsync();

    [Delete("/api/global-config/background-image/{id}")]
    Task DeleteBackgroundImageAsync(string id);
}
