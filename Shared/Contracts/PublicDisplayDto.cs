namespace Shared.Contracts;

public record PublicDisplayDto(
    List<PublicOfficeDto> Offices,
    List<AnnouncementDto> Announcements,
    WeatherDto? Weather,
    bool HasBackgroundImage,
    string? TodayHoliday,
    string? KioskName,
    int RefreshIntervalSeconds,
    DateTimeOffset GeneratedAt);