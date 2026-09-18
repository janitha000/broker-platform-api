namespace Identity.Domain.Tenants;

public static class BrokerPermissions
{
    public const string CasesRead = "cases:read";
    public const string CasesCreate = "cases:create";
    public const string CasesFactFind = "cases:fact-find";
    public const string CasesFactFindAny = "cases:fact-find-any";
    public const string CasesLodge = "cases:lodge";
    public const string CasesSettle = "cases:settle";
    public const string DocumentsRead = "documents:read";
    public const string DocumentsUpload = "documents:upload";
    public const string DocumentsSensitiveRead = "documents:sensitive-read";
    public const string AuditRead = "audit:read";

    public const string ClaimType = "permissions";
    public const string RoleClaimType = "role";

    public static IReadOnlyList<string> ForRole(string role) => role switch
    {
        BrokerRole.Principal =>
        [
            CasesRead,
            CasesCreate,
            CasesFactFind,
            CasesFactFindAny,
            CasesLodge,
            CasesSettle,
            DocumentsRead,
            DocumentsUpload,
            DocumentsSensitiveRead,
            AuditRead,
        ],
        BrokerRole.Assistant =>
        [
            CasesRead,
            CasesCreate,
            CasesFactFind,
            DocumentsRead,
            DocumentsUpload,
        ],
        BrokerRole.ReadOnly =>
        [
            CasesRead,
            DocumentsRead,
        ],
        _ => [],
    };
}