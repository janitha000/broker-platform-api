namespace Document.Domain.Documents;

public enum DocumentStatus
{
    PendingUpload,
    PendingScan,
    Clean,
    Quarantined,
    Rejected
}
