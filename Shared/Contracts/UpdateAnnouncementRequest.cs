namespace Shared.Contracts;

public record UpdateAnnouncementRequest(string Title, string Content, bool IsActive, DateTimeOffset? ExpiresAt);