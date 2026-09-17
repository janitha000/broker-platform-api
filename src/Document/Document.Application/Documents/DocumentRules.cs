namespace Document.Application.Documents;

public static class DocumentRules
{
    public const long MaxBytes = 20 * 1024 * 1024;

    public static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
    };

    public static bool IsSha256(string value) =>
        value.Length == 64 && value.All(Uri.IsHexDigit);
}