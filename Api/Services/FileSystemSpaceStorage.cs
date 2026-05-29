using System.Text.Json;
using Api.Interfaces;
using Shared.Contracts;

namespace Api.Services;

public sealed class FileSystemSpaceStorage(ILogger<FileSystemSpaceStorage> logger) : ISpaceStorage
{
    private static readonly string StorageDir = Path.Combine(AppContext.BaseDirectory, "backgrounds");
    private static readonly string MetadataFile = Path.Combine(StorageDir, "metadata.json");

    public async Task UploadAsync(Stream stream, string contentType, string originalFileName, string uploadedBy)
    {
        Directory.CreateDirectory(StorageDir);

        var id = Guid.NewGuid().ToString();
        var fileName = id;
        var filePath = Path.Combine(StorageDir, fileName);

        await using var fs = File.Create(filePath);
        await stream.CopyToAsync(fs);

        var entries = await LoadMetadataAsync();
        entries.Add(new ImageEntry(id, fileName, originalFileName, contentType, uploadedBy, DateTimeOffset.UtcNow));
        await SaveMetadataAsync(entries);

        logger.LogInformation("Background image saved to disk: {File} (original: {Original}, by: {User})", fileName, originalFileName, uploadedBy);
    }

    public async Task<(byte[] Data, string ContentType, string OriginalFileName)?> GetRandomAsync()
    {
        var entries = await LoadMetadataAsync();
        if (entries.Count == 0)
        {
            return null;
        }

        var picked = entries[Random.Shared.Next(entries.Count)];
        var filePath = Path.Combine(StorageDir, picked.FileName);
        if (!File.Exists(filePath))
        {
            return null;
        }

        var data = await File.ReadAllBytesAsync(filePath);
        return (data, picked.ContentType, picked.OriginalFileName);
    }

    public async Task<List<BackgroundImageDto>> ListAsync()
    {
        var entries = await LoadMetadataAsync();
        return entries
            .OrderByDescending(e => e.UploadedAt)
            .Select(e => new BackgroundImageDto(e.Id, e.OriginalFileName, e.ContentType, e.UploadedBy, e.UploadedAt))
            .ToList();
    }

    public async Task DeleteAsync(string id)
    {
        var entries = await LoadMetadataAsync();
        var entry = entries.FirstOrDefault(e => e.Id == id);
        if (entry is null)
        {
            return;
        }

        var filePath = Path.Combine(StorageDir, entry.FileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        entries.Remove(entry);
        await SaveMetadataAsync(entries);

        logger.LogInformation("Background image deleted: {File} (original: {Original})", entry.FileName, entry.OriginalFileName);
    }

    private static async Task<List<ImageEntry>> LoadMetadataAsync()
    {
        if (!File.Exists(MetadataFile))
        {
            return [];
        }

        var json = await File.ReadAllTextAsync(MetadataFile);
        return JsonSerializer.Deserialize<List<ImageEntry>>(json) ?? [];
    }

    private static async Task SaveMetadataAsync(List<ImageEntry> entries)
    {
        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(MetadataFile, json);
    }

    private sealed record ImageEntry(string Id, string FileName, string OriginalFileName, string ContentType, string UploadedBy, DateTimeOffset UploadedAt);
}
