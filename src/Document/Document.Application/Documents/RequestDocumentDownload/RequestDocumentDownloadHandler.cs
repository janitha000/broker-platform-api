using Document.Application.Abstractions;
using Document.Application.Auth;
using Document.Domain.Abstractions;
using Document.Domain.Documents;

namespace Document.Application.Documents.RequestDocumentDownload;

public sealed class RequestDocumentDownloadHandler
{
    private static readonly TimeSpan DownloadTtl = TimeSpan.FromMinutes(2);

    private readonly ICaseDocumentRepository _documents;
    private readonly IDocumentAccessLogRepository _accessLogs;
    private readonly IObjectStore _objectStore;
    private readonly ICurrentBroker _currentBroker;
    private readonly IUnitOfWork _unitOfWork;

    public RequestDocumentDownloadHandler(
        ICaseDocumentRepository documents,
        IDocumentAccessLogRepository accessLogs,
        IObjectStore objectStore,
        ICurrentBroker currentBroker,
        IUnitOfWork unitOfWork)
    {
        _documents = documents;
        _accessLogs = accessLogs;
        _objectStore = objectStore;
        _currentBroker = currentBroker;
        _unitOfWork = unitOfWork;
    }

    public async Task<RequestDocumentDownloadOutcome> Handle(
        RequestDocumentDownloadQuery query,
        CancellationToken cancellationToken = default)
    {
        var document = await _documents.GetById(
            query.DocumentId,
            _currentBroker.TenantId,
            cancellationToken);

        if (document is null)
            return new RequestDocumentDownloadOutcome(RequestDocumentDownloadKind.NotFound, null, null);

        var canReadSensitive = _currentBroker.HasPermission(DocumentPermissions.SensitiveRead);
        if (document.Sensitivity == DocumentSensitivity.Sensitive && !canReadSensitive)
            return new RequestDocumentDownloadOutcome(RequestDocumentDownloadKind.Forbidden, null, null);

        if (document.Status != DocumentStatus.Clean || string.IsNullOrEmpty(document.CleanKey))
        {
            return new RequestDocumentDownloadOutcome(
                RequestDocumentDownloadKind.Conflict,
                null,
                "Document is not available for download.");
        }

        var grant = _objectStore.CreateDownloadGrant(document.CleanKey, DownloadTtl);
        await _accessLogs.Add(
            DocumentAccess.Log(document, _currentBroker.BrokerId, DocumentAccessAction.DownloadUrlIssued),
            cancellationToken);
        await _unitOfWork.SaveChanges(cancellationToken);

        return new RequestDocumentDownloadOutcome(
            RequestDocumentDownloadKind.Succeeded,
            new RequestDocumentDownloadResult(grant.Url, grant.Token),
            null);
    }
}