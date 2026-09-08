namespace Identity.Domain.Registration;

public static class RegistrationSagaStatus
{
    public const string Started = "Started";
    public const string Charged = "Charged";
    public const string TenantCreated = "TenantCreated";
    public const string Completed = "Completed";
    public const string Compensating = "Compensating";
    public const string Compensated = "Compensated";
    public const string Failed = "Failed";
}

public sealed class RegistrationSaga
{
    public Guid Id { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = RegistrationSagaStatus.Started;
    public Guid? ChargeId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? BrokerUserId { get; set; }
    public string? Auth0UserId { get; set; }
    public int AttemptCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? LastError { get; set; }
}