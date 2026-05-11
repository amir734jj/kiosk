using Shared.Contracts;

namespace Api.Interfaces;

public interface IOfficeService
{
    Task<List<OfficeDto>> GetAllAsync();
    Task<OfficeDto?> GetByIdAsync(int id);
    Task<OfficeDto> CreateAsync(OfficeRequest req);
    Task<bool> UpdateAsync(int id, OfficeRequest req);
    Task<bool> UpdateMyAsync(int officeId, OfficeRequest req);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsByUnitAsync(string unitNumber);
    Task<bool> ExistsByUnitAsync(string unitNumber, int excludeId);
}
