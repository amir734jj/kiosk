namespace Shared.Contracts;

public record AnnouncementDto(
    int Id,
    string Title,
    string Content,
    bool IsActive,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt);