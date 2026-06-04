namespace Shared.Contracts;

public record AdvertisementPhotoDto(string Id, string OriginalFileName, string ContentType, DateTimeOffset UploadedAt);
