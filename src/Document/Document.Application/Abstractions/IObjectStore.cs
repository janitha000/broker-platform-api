namespace Document.Application.Abstractions;

public sealed record BlobGrant(string Url, string Token, string Method);

public interface IObjectStore
{
    string LandingKey(Guid tenantId, Guid caseId, Guid documentId, string fileName);
    string CleanKey(Guid tenantId, Guid caseId, Guid documentId, string fileName);
    BlobGrant CreateUploadGrant(Guid documentId, Guid tenantId, TimeSpan ttl);
    BlobGrant CreateDownloadGrant(string cleanKey, TimeSpan ttl);
    Task WriteLanding(Guid documentId, Guid tenantId, Stream content, CancellationToken ct);
    Task<bool> LandingExists(string landingKey, CancellationToken ct);
    Task<byte[]> ReadLandingBytes(string landingKey, CancellationToken ct);
    Task PromoteToClean(string landingKey, string cleanKey, CancellationToken ct);
    Task MoveToQuarantine(string landingKey, CancellationToken ct);
    Task<Stream> OpenClean(string cleanKey, CancellationToken ct);
}