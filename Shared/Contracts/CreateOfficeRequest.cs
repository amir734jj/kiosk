namespace Shared.Contracts;

public record CreateOfficeRequest(string UnitNumber, string Name, List<string>? Names, string? PhoneNumber, string? Note);