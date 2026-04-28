namespace Shared.Contracts;

public record PublicOfficeDto(string UnitNumber, string Name, List<string> Names, string? PhoneNumber, string? Note);