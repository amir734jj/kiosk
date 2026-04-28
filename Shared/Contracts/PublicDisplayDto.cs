namespace Shared.Contracts;

public record PublicDisplayDto(
    List<PublicOfficeDto> Offices,
    List<AnnouncementDto> Announcements,
    WeatherDto? Weather,
    string? BackgroundImageUrl,
    string? TodayHoliday,
    string? KioskName,
    DateTimeOffset GeneratedAt);