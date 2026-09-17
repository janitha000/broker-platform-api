using Amazon.S3;
using Amazon.S3.Model;
using Audit.Application.Abstractions;
using Audit.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Audit.Infrastructure.Storage;

public sealed class S3AuditArchive : IAuditArchive
{
    private readonly IAmazonS3 _s3;
    private readonly ArchiveOptions _options;
    private readonly ILogger<S3AuditArchive> _logger;

    public S3AuditArchive(IAmazonS3 s3, IOptions<ArchiveOptions> options, ILogger<S3AuditArchive> logger)
    {
        _s3 = s3;
        _options = options.Value;
        _logger = logger;
    }

    public async Task Put(AuditEventRecord record, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.Bucket))
        {
            _logger.LogInformation("Archive:Bucket empty; skip WORM put {EventId}", record.EventId);
            return;
        }

        var key = $"{record.TenantId:D}/{record.OccurredAt:yyyy/MM/dd}/{record.EventId:D}.json";
        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            ContentBody = record.EnvelopeJson,
            ContentType = "application/json",
        }, ct);
    }
}
