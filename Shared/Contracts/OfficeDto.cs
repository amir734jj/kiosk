namespace Shared.Contracts;

public record OfficeDto(
    int Id,
    int? Floor,
    string UnitNumber,
    string Name,
    List<string> Names,
    string? PhoneNumber,
    string? Note,
    DateTimeOffset CreatedAt);
