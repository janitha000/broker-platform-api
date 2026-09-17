using Document.Application.Abstractions;
using Document.Domain.Abstractions;
using Document.Domain.Documents;

namespace Document.Application.Documents.ScanPendingDocuments;

public sealed class ScanPendingDocumentsHandler
{
    private readonly ICaseDocumentRepository _documents;
    private readonly IDocumentAccessLogRepository _accessLogs;
    private readonly IObjectStore _objectStore;
    private readonly IMalwareScanner _scanner;
    private readonly IUnitOfWork _unitOfWork;

    public ScanPendingDocumentsHandler(
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

    public async Task Handle(CancellationToken cancellationToken = default)
    {
        var pending = await _documents.ListPendingScan(20, cancellationToken);

        foreach (var document in pending)
        {
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
                    DocumentAccess.Log(
                        document,
                        brokerId: null,
                        DocumentAccessAction.ScanSucceeded),
                    cancellationToken);
            }
            else if (result.Verdict == ScanVerdict.Threat)
            {
                await _objectStore.MoveToQuarantine(document.LandingKey, cancellationToken);
                document.Status = DocumentStatus.Quarantined;
                document.RejectionReason = result.Reason;
                await _accessLogs.Add(
                    DocumentAccess.Log(
                        document,
                        brokerId: null,
                        DocumentAccessAction.Quarantined,
                        result.Reason),
                    cancellationToken);
            }
            else
            {
                document.Status = DocumentStatus.Rejected;
                document.RejectionReason = result.Reason;
                await _accessLogs.Add(
                    DocumentAccess.Log(
                        document,
                        brokerId: null,
                        DocumentAccessAction.ScanRejected,
                        result.Reason),
                    cancellationToken);
            }

            await _documents.Update(document, cancellationToken);
        }

        if (pending.Count > 0)
            await _unitOfWork.SaveChanges(cancellationToken);
    }
}