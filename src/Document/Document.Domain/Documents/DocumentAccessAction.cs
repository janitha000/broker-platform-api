namespace Document.Domain.Documents;

public enum DocumentAccessAction
{
    UploadUrlIssued,
    UploadCompleted,
    DownloadUrlIssued,
    ScanSucceeded,
    Quarantined,
    ScanRejected
}