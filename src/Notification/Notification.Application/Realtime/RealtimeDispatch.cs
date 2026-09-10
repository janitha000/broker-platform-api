using Notification.Application.Abstractions;
using Notification.Application.Notifications.SendNotification;

namespace Notification.Application.Realtime;

public static class RealtimeDispatch
{
    public static RealtimeNotification? ForSent(
        SendNotificationCommand command,
        SendNotificationOutcome outcome)
    {
        if (outcome.Kind is not SendNotificationKind.Sent)
            return null;

        if (command.TenantId is not { } tenantId || tenantId == Guid.Empty)
            return null;

        Guid? caseId = command.CaseId;
        if (caseId is null
            && command.Data is not null
            && command.Data.TryGetValue("caseId", out var raw)
            && Guid.TryParse(raw, out var parsed))
        {
            caseId = parsed;
        }

        return new RealtimeNotification(
            command.TemplateKey,
            tenantId,
            caseId,
            outcome.Notification?.NotificationId);
    }
}
