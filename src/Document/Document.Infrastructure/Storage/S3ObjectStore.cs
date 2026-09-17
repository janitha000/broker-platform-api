using Amazon.S3;
using Amazon.S3.Model;
using Document.Application.Abstractions;
using Document.Domain.Documents;
using Microsoft.Extensions.Options;

namespace Document.Infrastructure.Storage;

public sealed class S3ObjectStore : IObjectStore
{
    private readonly IAmazonS3 _s3;
    private readonly S3StorageOptions _options;
    private readonly ICaseDocumentRepository _documents;

    public S3ObjectStore(
        IAmazonS3 s3,
        IOptions<S3StorageOptions> options,
        ICaseDocumentRepository documents)
    {
        _s3 = s3;
        _options = options.Value;
        _documents = documents;
    }

    public string LandingKey(Guid tenantId, Guid caseId, Guid documentId, string fileName) =>
        $"{tenantId:D}/{caseId:D}/{documentId:D}/{fileName}";

    public string CleanKey(Guid tenantId, Guid caseId, Guid documentId, string fileName) =>
        LandingKey(tenantId, caseId, documentId, fileName);

    public BlobGrant CreateUploadGrant(Guid documentId, Guid tenantId, TimeSpan ttl)
    {
        var document = _documents
            .GetById(documentId, tenantId)
            .GetAwaiter()
            .GetResult()
            ?? throw new InvalidOperationException("Document not found for upload grant.");

        var url = _s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.LandingBucket,
            Key = document.LandingKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(ttl),
            ContentType = document.ContentType,
        });

        return new BlobGrant(url, Token: string.Empty, Method: "PUT");
    }

    public BlobGrant CreateDownloadGrant(string cleanKey, TimeSpan ttl)
    {
        var url = _s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.CleanBucket,
            Key = cleanKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(ttl),
        });

        return new BlobGrant(url, Token: string.Empty, Method: "GET");
    }

    public async Task WriteLanding(
        Guid documentId,
        Guid tenantId,
        Stream content,
        CancellationToken ct)
    {
        var document = await _documents.GetById(documentId, tenantId, ct)
            ?? throw new InvalidOperationException("Document not found for landing write.");

        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.LandingBucket,
            Key = document.LandingKey,
            InputStream = content,
            ContentType = document.ContentType,
        }, ct);
    }

    public async Task<bool> LandingExists(string landingKey, CancellationToken ct)
    {
        try
        {
            await _s3.GetObjectMetadataAsync(_options.LandingBucket, landingKey, ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<byte[]> ReadLandingBytes(string landingKey, CancellationToken ct)
    {
        using var response = await _s3.GetObjectAsync(_options.LandingBucket, landingKey, ct);
        using var memory = new MemoryStream();
        await response.ResponseStream.CopyToAsync(memory, ct);
        return memory.ToArray();
    }

    public Task PromoteToClean(string landingKey, string cleanKey, CancellationToken ct) =>
        CopyThenDelete(landingKey, _options.CleanBucket, cleanKey, ct);

    public Task MoveToQuarantine(string landingKey, CancellationToken ct) =>
        CopyThenDelete(landingKey, _options.QuarantineBucket, landingKey, ct);

    public async Task<Stream> OpenClean(string cleanKey, CancellationToken ct)
    {
        var response = await _s3.GetObjectAsync(_options.CleanBucket, cleanKey, ct);
        return response.ResponseStream;
    }

    private async Task CopyThenDelete(
        string landingKey,
        string destinationBucket,
        string destinationKey,
        CancellationToken ct)
    {
        await _s3.CopyObjectAsync(new CopyObjectRequest
        {
            SourceBucket = _options.LandingBucket,
            SourceKey = landingKey,
            DestinationBucket = destinationBucket,
            DestinationKey = destinationKey,
        }, ct);

        await _s3.DeleteObjectAsync(_options.LandingBucket, landingKey, ct);
    }
}