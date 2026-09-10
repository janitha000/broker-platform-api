using Notification.Application.Notifications.SendNotification;
using Notification.Application.Realtime;

namespace Notification.Application.Tests.Realtime;

public sealed class RealtimeDispatchTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CaseId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid NotificationId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public void ForSent_WhenSentWithTenant_ReturnsPayload()
    {
        var command = Command(tenantId: TenantId, caseId: CaseId);
        var outcome = new SendNotificationOutcome(
            SendNotificationKind.Sent,
            new SendNotificationResult(NotificationId, "Sent"));

        var payload = RealtimeDispatch.ForSent(command, outcome);

        Assert.NotNull(payload);
        Assert.Equal("case.fact-find-completed", payload.Type);
        Assert.Equal(TenantId, payload.TenantId);
        Assert.Equal(CaseId, payload.CaseId);
        Assert.Equal(NotificationId, payload.NotificationId);
    }

    [Fact]
    public void ForSent_WhenCaseIdMissing_UsesData()
    {
        var command = Command(tenantId: TenantId, caseId: null);
        var outcome = new SendNotificationOutcome(SendNotificationKind.Sent, null);

        var payload = RealtimeDispatch.ForSent(command, outcome);

        Assert.Equal(CaseId, payload!.CaseId);
    }

    [Fact]
    public void ForSent_WhenConflict_ReturnsNull()
    {
        var command = Command(tenantId: TenantId, caseId: CaseId);
        var outcome = new SendNotificationOutcome(SendNotificationKind.IdempotencyConflict, null);

        Assert.Null(RealtimeDispatch.ForSent(command, outcome));
    }

    [Fact]
    public void ForSent_WhenTenantMissing_ReturnsNull()
    {
        var command = Command(tenantId: null, caseId: CaseId);
        var outcome = new SendNotificationOutcome(
            SendNotificationKind.Sent,
            new SendNotificationResult(NotificationId, "Sent"));

        Assert.Null(RealtimeDispatch.ForSent(command, outcome));
    }

    private static SendNotificationCommand Command(Guid? tenantId, Guid? caseId) =>
        new(
            "Email",
            "broker@example.com",
            "case.fact-find-completed",
            new Dictionary<string, string> { ["caseId"] = CaseId.ToString() },
            "origination",
            "key-1",
            "corr-1",
            tenantId,
            caseId);
}
