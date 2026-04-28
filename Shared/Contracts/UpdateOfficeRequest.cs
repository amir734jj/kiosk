namespace Shared.Contracts;

public record UpdateOfficeRequest(string UnitNumber, string Name, List<string>? Names, string? PhoneNumber, string? Note);