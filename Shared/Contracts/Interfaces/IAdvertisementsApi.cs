using Refit;

namespace Shared.Contracts.Interfaces;

[Headers("Authorization: Bearer")]
public interface IAdvertisementsApi
{
    [Get("/api/advertisements")]
    Task<List<AdvertisementDto>> GetAllAsync();

    [Get("/api/advertisements/{id}")]
    Task<AdvertisementDto> GetByIdAsync(int id);

    [Post("/api/advertisements")]
    Task<AdvertisementDto> CreateAsync([Body] CreateAdvertisementRequest request);

    [Put("/api/advertisements/{id}")]
    Task UpdateAsync(int id, [Body] UpdateAdvertisementRequest request);

    [Delete("/api/advertisements/{id}")]
    Task DeleteAsync(int id);

    [Multipart]
    [Post("/api/advertisements/{id}/photos")]
    Task<AdvertisementPhotoDto> UploadPhotoAsync(int id, [AliasAs("file")] StreamPart file);

    [Get("/api/advertisements/{id}/photos")]
    Task<List<AdvertisementPhotoDto>> GetPhotosAsync(int id);

    [Delete("/api/advertisements/{id}/photos/{photoId}")]
    Task DeletePhotoAsync(int id, string photoId);
}
