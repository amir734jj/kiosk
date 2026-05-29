namespace Shared.Contracts;

public record BackgroundImageDto(string Id, string OriginalFileName, string ContentType, string UploadedBy, DateTimeOffset UploadedAt);
