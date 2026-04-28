using Api.Data.Entities;
using Api.Interfaces;
using EfCoreRepository.Interfaces;
using Shared.Contracts;
using EfCoreRepository.Extensions;

namespace Api.Services;

public sealed class AnnouncementService(IEfRepository repository) : IAnnouncementService
{
    private IBasicCrud<Announcement> Dal => repository.For<Announcement>();

    private static AnnouncementDto ToDto(Announcement a) =>
        new(a.Id, a.Title, a.Content, a.IsActive, a.ExpiresAt, a.CreatedAt);

    public async Task<List<AnnouncementDto>> GetAllAsync()
    {
        return (await Dal.GetAll(
            project: a => ToDto(a))).ToList();
    }

    public async Task<List<AnnouncementDto>> GetActiveAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var all = (await Dal.GetAll(
            filterExprs: [a => a.IsActive],
            project: a => ToDto(a))).ToList();

        return all.Where(a => a.ExpiresAt == null || a.ExpiresAt > now).ToList();
    }

    public async Task<AnnouncementDto> CreateAsync(CreateAnnouncementRequest req)
    {
        var entity = await Dal.Save(new Announcement
        {
            Title = req.Title.Trim(),
            Content = req.Content.Trim(),
            ExpiresAt = req.ExpiresAt
        });
        return ToDto(entity);
    }

    public async Task<bool> UpdateAsync(int id, UpdateAnnouncementRequest req)
    {
        var items = (await Dal.GetAll(
            filterExprs: [a => a.Id == id],
            maxResults: 1)).ToList();

        if (items.Count == 0)
        {
            return false;
        }

        await Dal.Update(items.First().Id, a =>
        {
            a.Title = req.Title.Trim();
            a.Content = req.Content.Trim();
            a.IsActive = req.IsActive;
            a.ExpiresAt = req.ExpiresAt;
        });
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var items = (await Dal.GetAll(
            filterExprs: [a => a.Id == id],
            maxResults: 1)).ToList();

        if (items.Count == 0)
        {
            return false;
        }

        await Dal.Delete(items.First().Id);
        return true;
    }
}
