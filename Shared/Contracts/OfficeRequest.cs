using System.ComponentModel.DataAnnotations;

namespace Shared.Contracts;

public record OfficeRequest(
    string UnitNumber,
    string Name,
    List<string>? Names,
    string? PhoneNumber,
    string? Note,
    [Range(1, int.MaxValue, ErrorMessage = "Floor must be 1 or greater.")]
    int? Floor);
