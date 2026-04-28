using Shared.Contracts;

namespace Api.Interfaces;

public interface IAnnouncementService
{
    Task<List<AnnouncementDto>> GetAllAsync();
    Task<List<AnnouncementDto>> GetActiveAsync();
    Task<AnnouncementDto> CreateAsync(CreateAnnouncementRequest req);
    Task<bool> UpdateAsync(int id, UpdateAnnouncementRequest req);
    Task<bool> DeleteAsync(int id);
}
