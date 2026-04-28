using Refit;

namespace Shared.Contracts.Interfaces;

[Headers("Authorization: Bearer")]
public interface IOfficesApi
{
    [Get("/api/offices")]
    Task<List<OfficeDto>> GetAllAsync();

    [Get("/api/offices/{id}")]
    Task<OfficeDto> GetByIdAsync(int id);

    [Get("/api/offices/my")]
    Task<OfficeDto> GetMyAsync();

    [Post("/api/offices")]
    Task<OfficeDto> CreateAsync([Body] CreateOfficeRequest request);

    [Put("/api/offices/{id}")]
    Task UpdateAsync(int id, [Body] UpdateOfficeRequest request);

    [Put("/api/offices/my")]
    Task UpdateMyAsync([Body] UpdateOfficeRequest request);

    [Delete("/api/offices/{id}")]
    Task DeleteAsync(int id);

    [Post("/api/offices/{officeId}/assign-user/{userId}")]
    Task AssignUserAsync(int officeId, int userId);

    [Post("/api/offices/unassign-user/{userId}")]
    Task UnassignUserAsync(int userId);
}
