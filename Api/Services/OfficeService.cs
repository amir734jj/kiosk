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
        new(o.Id, o.Floor, o.UnitNumber, o.Name, o.Names, PhoneUtility.FormatForDisplay(o.PhoneNumber), o.Note, o.CreatedAt);

    public async Task<List<OfficeDto>> GetAllAsync()
    {
        return (await Dal.GetAll(project: o => ToDto(o)))
            .OrderBy(o => o.UnitNumber, NaturalSortComparer.Instance)
            .ToList();
    }

    public async Task<OfficeDto?> GetByIdAsync(int id)
    {
        var items = (await Dal.GetAll(
            filterExprs: [o => o.Id == id],
            maxResults: 1)).ToList();

        return items.Count > 0 ? ToDto(items.First()) : null;
    }

    public async Task<OfficeDto> CreateAsync(OfficeRequest req)
    {
        var entity = await Dal.Save(new Office
        {
            Floor = req.Floor,
            UnitNumber = req.UnitNumber.Trim(),
            Name = req.Name.Trim(),
            Names = req.Names ?? [],
            PhoneNumber = PhoneUtility.NormalizePhoneNumber(req.PhoneNumber),
            Note = req.Note?.Trim()
        });
        return ToDto(entity);
    }

    public async Task<bool> UpdateAsync(int id, OfficeRequest req)
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
            o.Floor = req.Floor;
            o.UnitNumber = req.UnitNumber.Trim();
            o.Name = req.Name.Trim();
            o.Names = req.Names ?? [];
            o.PhoneNumber = PhoneUtility.NormalizePhoneNumber(req.PhoneNumber);
            o.Note = req.Note?.Trim();
        });
        return true;
    }

    public async Task<bool> UpdateMyAsync(int officeId, OfficeRequest req)
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
            o.Floor = req.Floor;
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

}
