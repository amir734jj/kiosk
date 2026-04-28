using Api.Data.Entities;
using Api.Interfaces;
using Api.Utilities;
using EfCoreRepository.Interfaces;
using Shared.Contracts;
using EfCoreRepository.Extensions;

namespace Api.Services;

public sealed class OfficeService(IEfRepository repository) : IOfficeService
{
    private IBasicCrud<Office> Dal => repository.For<Office>();

    private static OfficeDto ToDto(Office o) =>
        new(o.Id, o.UnitNumber, o.Name, o.Names, PhoneUtility.FormatForDisplay(o.PhoneNumber), o.Note, o.CreatedAt);

    public async Task<List<OfficeDto>> GetAllAsync()
    {
        return (await Dal.GetAll(
            orderBy: o => o.UnitNumber,
            project: o => ToDto(o))).ToList();
    }

    public async Task<OfficeDto?> GetByIdAsync(int id)
    {
        var items = (await Dal.GetAll(
            filterExprs: [o => o.Id == id],
            maxResults: 1)).ToList();

        return items.Count > 0 ? ToDto(items.First()) : null;
    }

    public async Task<OfficeDto> CreateAsync(CreateOfficeRequest req)
    {
        var entity = await Dal.Save(new Office
        {
            UnitNumber = req.UnitNumber.Trim(),
            Name = req.Name.Trim(),
            Names = req.Names ?? [],
            PhoneNumber = PhoneUtility.NormalizePhoneNumber(req.PhoneNumber),
            Note = req.Note?.Trim()
        });
        return ToDto(entity);
    }

    public async Task<bool> UpdateAsync(int id, UpdateOfficeRequest req)
    {
        var items = (await Dal.GetAll(
            filterExprs: [o => o.Id == id],
            maxResults: 1)).ToList();

        if (items.Count == 0)
        {
            return false;
        }

        await Dal.Update(items.First().Id, o =>
        {
            o.UnitNumber = req.UnitNumber.Trim();
            o.Name = req.Name.Trim();
            o.Names = req.Names ?? [];
            o.PhoneNumber = PhoneUtility.NormalizePhoneNumber(req.PhoneNumber);
            o.Note = req.Note?.Trim();
        });
        return true;
    }

    public async Task<bool> UpdateMyAsync(int officeId, UpdateOfficeRequest req)
    {
        var items = (await Dal.GetAll(
            filterExprs: [o => o.Id == officeId],
            maxResults: 1)).ToList();

        if (items.Count == 0)
        {
            return false;
        }

        await Dal.Update(items.First().Id, o =>
        {
            o.Name = req.Name.Trim();
            o.Names = req.Names ?? [];
            o.PhoneNumber = PhoneUtility.NormalizePhoneNumber(req.PhoneNumber);
            o.Note = req.Note?.Trim();
        });
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var items = (await Dal.GetAll(
            filterExprs: [o => o.Id == id],
            maxResults: 1)).ToList();

        if (items.Count == 0)
        {
            return false;
        }

        await Dal.Delete(items.First().Id);
        return true;
    }

    public async Task<bool> ExistsByUnitAsync(string unitNumber)
    {
        return await Dal.Any([o => o.UnitNumber == unitNumber.Trim()]);
    }

    public async Task<bool> ExistsByUnitAsync(string unitNumber, int excludeId)
    {
        return await Dal.Any([o => o.UnitNumber == unitNumber.Trim() && o.Id != excludeId]);
    }
}
