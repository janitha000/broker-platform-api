namespace Broker.Hosting.Audit;

public static class AuditOutcomes
{
    public const string Allow = "allow";
    public const string Deny = "deny";
}

public static class AuditActions
{
    public const string AuthzDeny = "authz.deny";

    public const string CaseView = "case.view";
    public const string CaseFactFindComplete = "case.fact-find.complete";

    public const string DocumentUploadUrlIssued = "document.upload-url.issued";
    public const string DocumentUploadCompleted = "document.upload.completed";
    public const string DocumentDownloadUrlIssued = "document.download-url.issued";
    public const string DocumentScanSucceeded = "document.scan.succeeded";
    public const string DocumentQuarantined = "document.scan.quarantined";
    public const string DocumentScanRejected = "document.scan.rejected";

    public const string IdentityTenantRegistered = "identity.tenant.registered";
}

public static class AuditEventTypes
{
    /// <summary>EventBridge DetailType for every audit record.</summary>
    public const string AuditEvent = "AuditEvent";
}