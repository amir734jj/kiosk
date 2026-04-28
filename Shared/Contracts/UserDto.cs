namespace Shared.Contracts;

public record UserDto(int Id, string Username, string Role, bool IsActive, int? OfficeId, string? OfficeName, DateTimeOffset? LastLoginAt);