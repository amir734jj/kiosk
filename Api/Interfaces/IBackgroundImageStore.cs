namespace Api.Interfaces;

public interface IBackgroundImageStore
{
    Task UploadAsync(Stream stream, string contentType, string originalFileName, string uploadedBy);
    Task<(byte[] Data, string ContentType)?> GetLatestAsync();
}
