using Refit;

namespace Shared.Contracts.Interfaces;

[Headers("Authorization: Bearer")]
public interface IAnnouncementsApi
{
    [Get("/api/announcements")]
    Task<List<AnnouncementDto>> GetAllAsync();

    [Post("/api/announcements")]
    Task<AnnouncementDto> CreateAsync([Body] CreateAnnouncementRequest request);

    [Put("/api/announcements/{id}")]
    Task UpdateAsync(int id, [Body] UpdateAnnouncementRequest request);

    [Delete("/api/announcements/{id}")]
    Task DeleteAsync(int id);
}
