namespace Identity.Application.Abstractions;

public sealed class PaymentOptions
{
    public const string SectionName = "Payment";
    public string BaseUrl { get; set; } = string.Empty;
}