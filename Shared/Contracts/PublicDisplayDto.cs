namespace Shared.Contracts;

public record PublicDisplayDto(
    List<PublicOfficeDto> Offices,
    List<AnnouncementDto> Announcements,
    WeatherDto? Weather,
    bool HasBackgroundImage,
    string? TodayHoliday,
    string? KioskName,
    int RefreshIntervalSeconds,
    int AdIntervalSeconds,
    int AdDurationSeconds,
    DateTimeOffset GeneratedAt);