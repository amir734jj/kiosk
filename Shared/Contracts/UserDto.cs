namespace Shared.Contracts;

public record UserDto(int Id, string Email, string Role, bool IsActive, int? OfficeId, string? OfficeName, DateTimeOffset? LastLoginAt);