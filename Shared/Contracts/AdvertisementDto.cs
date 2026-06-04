namespace Shared.Contracts;

public record AdvertisementDto(
    int Id,
    string Title,
    string Description,
    bool IsActive,
    List<AdvertisementPhotoDto> Photos,
    DateTimeOffset CreatedAt);
