using Shared.Contracts;

namespace Api.Interfaces;

public interface IAdvertisementService
{
    Task<List<AdvertisementDto>> GetAllAsync();
    Task<List<AdvertisementDto>> GetActiveAsync();
    Task<AdvertisementDto?> GetByIdAsync(int id);
    Task<AdvertisementDto> CreateAsync(CreateAdvertisementRequest req);
    Task<bool> UpdateAsync(int id, UpdateAdvertisementRequest req);
    Task<bool> DeleteAsync(int id);
    Task<AdvertisementPhotoDto> UploadPhotoAsync(int adId, Stream stream, string contentType, string originalFileName);
    Task<List<AdvertisementPhotoDto>> GetPhotosAsync(int adId);
    Task<(byte[] Data, string ContentType, string OriginalFileName)?> GetPhotoAsync(int adId, string photoId);
    Task<bool> DeletePhotoAsync(int adId, string photoId);
}
