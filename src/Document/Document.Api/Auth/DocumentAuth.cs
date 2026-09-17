using System.Security.Claims;
using Document.Application.Auth;

namespace Document.Api.Auth;

public static class DocumentAuth
{
    public const string ReadPolicy = "DocumentsRead";
    public const string UploadPolicy = "DocumentsUpload";
    public const string SensitiveReadPolicy = "DocumentsSensitiveRead";

    public static bool HasPermission(ClaimsPrincipal user, string permission)
    {
        if (user.FindAll(DocumentPermissions.ClaimType).Any(c => c.Value == permission))
            return true;

        var scope = user.FindFirst("scope")?.Value;
        if (string.IsNullOrEmpty(scope))
            return false;

        return scope.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Contains(permission);
    }
}