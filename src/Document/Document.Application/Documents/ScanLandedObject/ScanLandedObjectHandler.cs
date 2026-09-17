using Document.Application.Abstractions;
using Document.Domain.Abstractions;
using Document.Domain.Documents;

namespace Document.Application.Documents.ScanLandedObject;

public sealed class ScanLandedObjectHandler
{
    private readonly ICaseDocumentRepository _documents;
    private readonly IDocumentAccessLogRepository _accessLogs;
    private readonly IObjectStore _objectStore;
    private readonly IMalwareScanner _scanner;
    private readonly IUnitOfWork _unitOfWork;

    public ScanLandedObjectHandler(
        ICaseDocumentRepository documents,
        IDocumentAccessLogRepository accessLogs,
        IObjectStore objectStore,
        IMalwareScanner scanner,
        IUnitOfWork unitOfWork)
    {
        _documents = documents;
        _accessLogs = accessLogs;
        _objectStore = objectStore;
        _scanner = scanner;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(string landingKey, CancellationToken cancellationToken = default)
    {
        var documentId = ParseDocumentId(landingKey);
        var document = await _documents.GetByIdIgnoringTenant(documentId, cancellationToken)
            ?? throw new DocumentNotReadyException($"No document row for {landingKey}.");

        if (document.Status is DocumentStatus.Clean
            or DocumentStatus.Quarantined
            or DocumentStatus.Rejected)
            return;

        if (document.Status != DocumentStatus.PendingScan)
        {
            throw new DocumentNotReadyException(
                $"Document {document.Id} is {document.Status}; waiting for Complete.");
        }

        var bytes = await _objectStore.ReadLandingBytes(document.LandingKey, cancellationToken);
        var result = _scanner.Scan(bytes, document.ContentType);
        document.ScannedAt = DateTime.UtcNow;

        if (result.Verdict == ScanVerdict.Clean)
        {
            document.CleanKey = _objectStore.CleanKey(
                document.TenantId,
                document.CaseId,
                document.Id,
                document.OriginalFileName);
            await _objectStore.PromoteToClean(
                document.LandingKey,
                document.CleanKey,
                cancellationToken);
            document.Status = DocumentStatus.Clean;
            await _accessLogs.Add(
                DocumentAccess.Log(document, null, DocumentAccessAction.ScanSucceeded),
                cancellationToken);
        }
        else if (result.Verdict == ScanVerdict.Threat)
        {
            await _objectStore.MoveToQuarantine(document.LandingKey, cancellationToken);
            document.Status = DocumentStatus.Quarantined;
            document.RejectionReason = result.Reason;
            await _accessLogs.Add(
                DocumentAccess.Log(document, null, DocumentAccessAction.Quarantined, result.Reason),
                cancellationToken);
        }
        else
        {
            document.Status = DocumentStatus.Rejected;
            document.RejectionReason = result.Reason;
            await _accessLogs.Add(
                DocumentAccess.Log(document, null, DocumentAccessAction.ScanRejected, result.Reason),
                cancellationToken);
        }

        await _documents.Update(document, cancellationToken);
        await _unitOfWork.SaveChanges(cancellationToken);
    }

    public static Guid ParseDocumentId(string landingKey)
    {
        var parts = landingKey.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3 || !Guid.TryParse(parts[2], out var documentId))
            throw new InvalidOperationException($"Landing key is not tenant/case/document/file: {landingKey}");

        return documentId;
    }
}