using System.ComponentModel.DataAnnotations;

namespace Shared.Contracts;

public record OfficeRequest(
    string UnitNumber,
    string Name,
    List<string>? Names,
    string? PhoneNumber,
    string? Note,
    [Range(0, int.MaxValue, ErrorMessage = "Floor must be 0 or greater.")] int? Floor
);
