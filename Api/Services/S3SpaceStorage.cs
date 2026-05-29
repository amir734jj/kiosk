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
        logger.LogDebug("S3 UploadAsync starting — bucket={Bucket}, key={Key}, contentType={ContentType}, originalFile={OriginalFile}, uploadedBy={User}, streamLength={Length}",
            BucketName, key, contentType, originalFileName, uploadedBy, stream.Length);

        var request = new PutObjectRequest
        {
            BucketName = BucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType,
            DisablePayloadSigning = true,
            Metadata =
            {
                ["x-amz-meta-original-filename"] = originalFileName,
                ["x-amz-meta-uploaded-at"] = DateTime.UtcNow.ToString("O"),
                ["x-amz-meta-uploaded-by"] = uploadedBy
            }
        };

        try
        {
            var response = await s3.PutObjectAsync(request);
            logger.LogDebug("S3 UploadAsync completed — key={Key}, httpStatus={Status}, eTag={ETag}",
                key, response.HttpStatusCode, response.ETag);
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogError(ex, "S3 UploadAsync FAILED — key={Key}, errorCode={ErrorCode}, statusCode={StatusCode}, message={Message}",
                key, ex.ErrorCode, ex.StatusCode, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "S3 UploadAsync FAILED (non-S3 error) — key={Key}", key);
            throw;
        }

        cache.Remove(CacheKey);
    }

    public async Task<(byte[] Data, string ContentType, string OriginalFileName)?> GetRandomAsync()
    {
        if (cache.TryGetValue(CacheKey, out (byte[] Data, string ContentType, string OriginalFileName) cached))
        {
            logger.LogDebug("S3 GetRandomAsync cache HIT — file={File}, size={Size} bytes",
                cached.OriginalFileName, cached.Data.Length);
            cache.Set(CacheKey, cached, TimeSpan.FromHours(1));
            return cached;
        }

        logger.LogDebug("S3 GetRandomAsync cache MISS — listing objects from bucket={Bucket}, prefix={Prefix}", BucketName, Prefix);

        try
        {
            var list = await s3.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = BucketName,
                Prefix = Prefix,
                MaxKeys = 100
            });

            logger.LogDebug("S3 GetRandomAsync ListObjectsV2 returned {Count} objects (IsTruncated={Truncated})",
                list.S3Objects?.Count ?? 0, list.IsTruncated);

            if (list.S3Objects is null || list.S3Objects.Count == 0)
            {
                logger.LogDebug("S3 GetRandomAsync — no objects found, returning null");
                return null;
            }

            var picked = list.S3Objects[Random.Shared.Next(list.S3Objects.Count)];
            logger.LogDebug("S3 GetRandomAsync — picked key={Key}, size={Size}, lastModified={LastModified}",
                picked.Key, picked.Size, picked.LastModified);

            var response = await s3.GetObjectAsync(BucketName, picked.Key);
            logger.LogDebug("S3 GetRandomAsync GetObject — httpStatus={Status}, contentType={ContentType}, contentLength={Length}",
                response.HttpStatusCode, response.Headers.ContentType, response.Headers.ContentLength);

            using var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms);

            var originalFileName = response.Metadata["x-amz-meta-original-filename"] ?? picked.Key[Prefix.Length..];
            var result = (Data: ms.ToArray(), ContentType: response.Headers.ContentType ?? "image/jpeg", OriginalFileName: originalFileName);

            logger.LogDebug("S3 GetRandomAsync — returning file={File}, contentType={ContentType}, dataSize={Size} bytes",
                result.OriginalFileName, result.ContentType, result.Data.Length);

            cache.Set(CacheKey, result, TimeSpan.FromHours(1));
            return result;
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogWarning(ex, "S3 GetRandomAsync FAILED — errorCode={ErrorCode}, statusCode={StatusCode}, message={Message}",
                ex.ErrorCode, ex.StatusCode, ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "S3 GetRandomAsync FAILED (non-S3 error)");
            return null;
        }
    }

    public async Task<(byte[] Data, string ContentType, string OriginalFileName)?> GetByIdAsync(string id)
    {
        var key = $"{Prefix}{id}";
        logger.LogDebug("S3 GetByIdAsync starting — bucket={Bucket}, key={Key}", BucketName, key);

        try
        {
            var response = await s3.GetObjectAsync(BucketName, key);
            using var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms);

            var originalFileName = response.Metadata["x-amz-meta-original-filename"] ?? id;
            var contentType = response.Headers.ContentType ?? "image/jpeg";

            logger.LogDebug("S3 GetByIdAsync — file={File}, contentType={ContentType}, size={Size} bytes",
                originalFileName, contentType, ms.Length);

            return (ms.ToArray(), contentType, originalFileName);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogDebug("S3 GetByIdAsync — key={Key} not found", key);
            return null;
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogWarning(ex, "S3 GetByIdAsync FAILED — key={Key}, errorCode={ErrorCode}", key, ex.ErrorCode);
            return null;
        }
    }

    public async Task<List<BackgroundImageDto>> ListAsync()
    {
        logger.LogDebug("S3 ListAsync starting — bucket={Bucket}, prefix={Prefix}", BucketName, Prefix);

        try
        {
            var list = await s3.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = BucketName,
                Prefix = Prefix,
                MaxKeys = 100
            });

            logger.LogDebug("S3 ListAsync ListObjectsV2 returned {Count} objects (IsTruncated={Truncated})",
                list.S3Objects?.Count ?? 0, list.IsTruncated);

            var results = new List<BackgroundImageDto>();
            foreach (var obj in (list.S3Objects ?? []).OrderByDescending(o => o.LastModified))
            {
                logger.LogDebug("S3 ListAsync fetching metadata for key={Key}, size={Size}, lastModified={LastModified}",
                    obj.Key, obj.Size, obj.LastModified);

                var meta = await s3.GetObjectMetadataAsync(BucketName, obj.Key);
                var id = obj.Key[Prefix.Length..];
                var originalName = meta.Metadata["x-amz-meta-original-filename"] ?? id;
                var uploadedBy = meta.Metadata["x-amz-meta-uploaded-by"] ?? "unknown";

                logger.LogDebug("S3 ListAsync — id={Id}, originalName={OriginalName}, contentType={ContentType}, uploadedBy={UploadedBy}",
                    id, originalName, meta.Headers.ContentType, uploadedBy);

                results.Add(new BackgroundImageDto(
                    id,
                    originalName,
                    meta.Headers.ContentType ?? "image/jpeg",
                    uploadedBy,
                    obj.LastModified ?? DateTimeOffset.UtcNow));
            }

            logger.LogDebug("S3 ListAsync completed — returning {Count} images", results.Count);
            return results;
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogWarning(ex, "S3 ListAsync FAILED — errorCode={ErrorCode}, statusCode={StatusCode}, message={Message}",
                ex.ErrorCode, ex.StatusCode, ex.Message);
            return [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "S3 ListAsync FAILED (non-S3 error)");
            return [];
        }
    }

    public async Task DeleteAsync(string id)
    {
        var key = $"{Prefix}{id}";
        logger.LogDebug("S3 DeleteAsync starting — bucket={Bucket}, key={Key}", BucketName, key);

        try
        {
            var response = await s3.DeleteObjectAsync(BucketName, key);
            logger.LogDebug("S3 DeleteAsync completed — key={Key}, httpStatus={Status}", key, response.HttpStatusCode);
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogError(ex, "S3 DeleteAsync FAILED — key={Key}, errorCode={ErrorCode}, statusCode={StatusCode}, message={Message}",
                key, ex.ErrorCode, ex.StatusCode, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "S3 DeleteAsync FAILED (non-S3 error) — key={Key}", key);
            throw;
        }

        cache.Remove(CacheKey);
    }
}
