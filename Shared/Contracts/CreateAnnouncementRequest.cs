namespace Shared.Contracts;

public record CreateAnnouncementRequest(string Title, string Content, DateTimeOffset? ExpiresAt);