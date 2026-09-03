namespace Identity.Application.Abstractions;

public sealed class Auth0Options
{
    public const string SectionName = "Auth0";

    public string Domain { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    /// <summary>SPA origin: http://localhost:5173 or https://d9oy49gmln888.cloudfront.net</summary>
    public string AppBaseUrl { get; set; } = string.Empty;
    public string ManagementClientId { get; set; } = string.Empty;
    public string ManagementClientSecret { get; set; } = string.Empty;
    public string DatabaseConnection { get; set; } = "Username-Password-Authentication";
    public string PaymentAudience { get; set; } = string.Empty;
    public string PaymentClientId { get; set; } = string.Empty;
    public string PaymentClientSecret { get; set; } = string.Empty;
}