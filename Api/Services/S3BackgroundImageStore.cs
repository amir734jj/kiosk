using Amazon.S3;
using Amazon.S3.Model;
using Api.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Api.Services;

public sealed class S3BackgroundImageStore(IAmazonS3 s3, IMemoryCache cache, ILogger<S3BackgroundImageStore> logger) : IBackgroundImageStore
{
    private const string BucketName = "kiosk";
    private const string Prefix = "backgrounds/";
    private const string CacheKey = "s3:background:latest";

    public async Task UploadAsync(Stream stream, string contentType, string originalFileName, string uploadedBy)
    {
        var key = $"{Prefix}{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";

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

    public async Task<(byte[] Data, string ContentType)?> GetLatestAsync()
    {
        if (cache.TryGetValue(CacheKey, out (byte[] Data, string ContentType) cached))
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

            var latest = list.S3Objects.OrderByDescending(o => o.LastModified).First();

            var response = await s3.GetObjectAsync(BucketName, latest.Key);
            using var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms);

            var result = (Data: ms.ToArray(), ContentType: response.Headers.ContentType ?? "image/jpeg");
            cache.Set(CacheKey, result, TimeSpan.FromHours(1));
            return result;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch background image from S3");
            return null;
        }
    }
}
