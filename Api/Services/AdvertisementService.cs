using Amazon.S3;
using Amazon.S3.Model;
using Api.Data.Entities;
using Api.Interfaces;
using EfCoreRepository.Interfaces;
using Shared.Contracts;

namespace Api.Services;

public sealed class AdvertisementService(IEfRepository repository, IAmazonS3 s3, ILogger<AdvertisementService> logger) : IAdvertisementService
{
    private const string BucketName = "kiosk";
    private const string Prefix = "ads/";

    private IBasicCrud<Advertisement> Dal => repository.For<Advertisement>();

    private async Task<AdvertisementDto> ToDtoAsync(Advertisement a)
    {
        var photos = await ListPhotosFromS3(a.Id);
        return new AdvertisementDto(a.Id, a.Title, a.Description, a.IsActive, photos, a.CreatedAt);
    }

    public async Task<List<AdvertisementDto>> GetAllAsync()
    {
        var entities = (await Dal.GetAll(project: a => a)).ToList();
        var results = new List<AdvertisementDto>();
        foreach (var e in entities)
            results.Add(await ToDtoAsync(e));
        return results;
    }

    public async Task<List<AdvertisementDto>> GetActiveAsync()
    {
        var entities = (await Dal.GetAll(filterExprs: [a => a.IsActive], project: a => a)).ToList();
        var results = new List<AdvertisementDto>();
        foreach (var e in entities)
            results.Add(await ToDtoAsync(e));
        return results;
    }

    public async Task<AdvertisementDto?> GetByIdAsync(int id)
    {
        var items = (await Dal.GetAll(filterExprs: [a => a.Id == id], project: a => a, maxResults: 1)).ToList();
        if (items.Count == 0) return null;
        return await ToDtoAsync(items.First());
    }

    public async Task<AdvertisementDto> CreateAsync(CreateAdvertisementRequest req)
    {
        var entity = await Dal.Save(new Advertisement
        {
            Title = req.Title.Trim(),
            Description = req.Description.Trim()
        });
        return await ToDtoAsync(entity);
    }

    public async Task<bool> UpdateAsync(int id, UpdateAdvertisementRequest req)
    {
        var items = (await Dal.GetAll(filterExprs: [a => a.Id == id], project: a => a, maxResults: 1)).ToList();
        if (items.Count == 0) return false;

        await Dal.Update(items.First().Id, a =>
        {
            a.Title = req.Title.Trim();
            a.Description = req.Description.Trim();
            a.IsActive = req.IsActive;
        });
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var items = (await Dal.GetAll(filterExprs: [a => a.Id == id], project: a => a, maxResults: 1)).ToList();
        if (items.Count == 0) return false;

        // Delete all photos from S3
        await DeleteAllPhotosFromS3(id);
        await Dal.Delete(items.First().Id);
        return true;
    }

    public async Task<AdvertisementPhotoDto> UploadPhotoAsync(int adId, Stream stream, string contentType, string originalFileName)
    {
        var photoId = Guid.NewGuid().ToString();
        var key = $"{Prefix}{adId}/{photoId}";

        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = BucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType,
            DisablePayloadSigning = true,
            Metadata =
            {
                ["x-amz-meta-original-filename"] = originalFileName,
                ["x-amz-meta-uploaded-at"] = DateTime.UtcNow.ToString("O")
            }
        });

        logger.LogInformation("Uploaded ad photo — adId={AdId}, photoId={PhotoId}, file={File}", adId, photoId, originalFileName);
        return new AdvertisementPhotoDto(photoId, originalFileName, contentType, DateTimeOffset.UtcNow);
    }

    public async Task<List<AdvertisementPhotoDto>> GetPhotosAsync(int adId)
    {
        return await ListPhotosFromS3(adId);
    }

    public async Task<(byte[] Data, string ContentType, string OriginalFileName)?> GetPhotoAsync(int adId, string photoId)
    {
        var key = $"{Prefix}{adId}/{photoId}";
        try
        {
            var response = await s3.GetObjectAsync(BucketName, key);
            using var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms);
            var originalFileName = response.Metadata["x-amz-meta-original-filename"] ?? photoId;
            return (ms.ToArray(), response.Headers.ContentType ?? "image/jpeg", originalFileName);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogWarning(ex, "Failed to get ad photo — adId={AdId}, photoId={PhotoId}", adId, photoId);
            return null;
        }
    }

    public async Task<bool> DeletePhotoAsync(int adId, string photoId)
    {
        var key = $"{Prefix}{adId}/{photoId}";
        try
        {
            await s3.DeleteObjectAsync(BucketName, key);
            logger.LogInformation("Deleted ad photo — adId={AdId}, photoId={PhotoId}", adId, photoId);
            return true;
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete ad photo — adId={AdId}, photoId={PhotoId}", adId, photoId);
            return false;
        }
    }

    private async Task<List<AdvertisementPhotoDto>> ListPhotosFromS3(int adId)
    {
        var prefix = $"{Prefix}{adId}/";
        try
        {
            var list = await s3.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = BucketName,
                Prefix = prefix,
                MaxKeys = 100
            });

            var results = new List<AdvertisementPhotoDto>();
            foreach (var obj in list.S3Objects ?? [])
            {
                var photoId = obj.Key[prefix.Length..];
                if (string.IsNullOrEmpty(photoId)) continue;

                var meta = await s3.GetObjectMetadataAsync(BucketName, obj.Key);
                var originalName = meta.Metadata["x-amz-meta-original-filename"] ?? photoId;
                var contentType = meta.Headers.ContentType ?? "image/jpeg";

                results.Add(new AdvertisementPhotoDto(photoId, originalName, contentType, obj.LastModified ?? DateTimeOffset.UtcNow));
            }
            return results;
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogWarning(ex, "Failed to list ad photos — adId={AdId}", adId);
            return [];
        }
    }

    private async Task DeleteAllPhotosFromS3(int adId)
    {
        var prefix = $"{Prefix}{adId}/";
        try
        {
            var list = await s3.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = BucketName,
                Prefix = prefix,
                MaxKeys = 100
            });

            foreach (var obj in list.S3Objects ?? [])
            {
                await s3.DeleteObjectAsync(BucketName, obj.Key);
            }
            logger.LogInformation("Deleted all photos for ad — adId={AdId}", adId);
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete all ad photos — adId={AdId}", adId);
        }
    }
}
