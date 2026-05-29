using Amazon.S3;
using Amazon.S3.Model;
using Api.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Shared.Contracts;

namespace Api.Services;

public sealed class S3SpaceStorage(IAmazonS3 s3, IMemoryCache cache, ILogger<S3SpaceStorage> logger) : ISpaceStorage
{
    private const string BucketName = "kiosk";
    private const string Prefix = "backgrounds/";
    private const string CacheKey = "s3:background:latest";

    public async Task UploadAsync(Stream stream, string contentType, string originalFileName, string uploadedBy)
    {
        var key = $"{Prefix}{Guid.NewGuid()}";

        var request = new PutObjectRequest
        {
            BucketName = BucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType,
            Metadata =
            {
                ["x-amz-meta-original-filename"] = originalFileName,
                ["x-amz-meta-uploaded-at"] = DateTime.UtcNow.ToString("O"),
                ["x-amz-meta-uploaded-by"] = uploadedBy
            }
        };

        await s3.PutObjectAsync(request);

        cache.Remove(CacheKey);
    }

    public async Task<(byte[] Data, string ContentType, string OriginalFileName)?> GetRandomAsync()
    {
        if (cache.TryGetValue(CacheKey, out (byte[] Data, string ContentType, string OriginalFileName) cached))
        {
            cache.Set(CacheKey, cached, TimeSpan.FromHours(1));
            return cached;
        }

        try
        {
            var list = await s3.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = BucketName,
                Prefix = Prefix,
                MaxKeys = 100
            });

            if (list.S3Objects.Count == 0)
            {
                return null;
            }

            var picked = list.S3Objects[Random.Shared.Next(list.S3Objects.Count)];

            var response = await s3.GetObjectAsync(BucketName, picked.Key);
            using var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms);

            var originalFileName = response.Metadata["x-amz-meta-original-filename"] ?? picked.Key[Prefix.Length..];
            var result = (Data: ms.ToArray(), ContentType: response.Headers.ContentType ?? "image/jpeg", OriginalFileName: originalFileName);
            cache.Set(CacheKey, result, TimeSpan.FromHours(1));
            return result;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch background image from S3");
            return null;
        }
    }

    public async Task<List<BackgroundImageDto>> ListAsync()
    {
        try
        {
            var list = await s3.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = BucketName,
                Prefix = Prefix,
                MaxKeys = 100
            });

            var results = new List<BackgroundImageDto>();
            foreach (var obj in list.S3Objects.OrderByDescending(o => o.LastModified))
            {
                var meta = await s3.GetObjectMetadataAsync(BucketName, obj.Key);
                var id = obj.Key[Prefix.Length..];
                results.Add(new BackgroundImageDto(
                    id,
                    meta.Metadata["x-amz-meta-original-filename"] ?? id,
                    meta.Headers.ContentType ?? "image/jpeg",
                    meta.Metadata["x-amz-meta-uploaded-by"] ?? "unknown",
                    obj.LastModified ?? DateTimeOffset.UtcNow));
            }

            return results;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to list background images from S3");
            return [];
        }
    }

    public async Task DeleteAsync(string id)
    {
        await s3.DeleteObjectAsync(BucketName, $"{Prefix}{id}");
        cache.Remove(CacheKey);
    }
}
