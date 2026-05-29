using Shared.Contracts;

namespace Api.Interfaces;

public interface ISpaceStorage
{
    Task UploadAsync(Stream stream, string contentType, string originalFileName, string uploadedBy);
    Task<(byte[] Data, string ContentType, string OriginalFileName)?> GetRandomAsync();
    Task<(byte[] Data, string ContentType, string OriginalFileName)?> GetByIdAsync(string id);
    Task<List<BackgroundImageDto>> ListAsync();
    Task DeleteAsync(string id);
}
