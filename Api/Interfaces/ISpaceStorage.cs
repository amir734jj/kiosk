namespace Api.Interfaces;

public interface ISpaceStorage
{
    Task UploadAsync(Stream stream, string contentType, string originalFileName, string uploadedBy);
    Task<(byte[] Data, string ContentType)?> GetRandomAsync();
}
