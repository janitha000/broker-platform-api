namespace Payment.Domain.Charges;

public static class ChargeStatus
{
    public const string Succeeded = "Succeeded";
    public const string Declined = "Declined";
    public const string Refunded = "Refunded";
}

public sealed class Charge
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string PayloadFingerprint { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? RefundedAt { get; set; }
}
