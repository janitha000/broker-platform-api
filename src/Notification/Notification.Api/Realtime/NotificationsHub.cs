using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Notification.Api.Realtime;

[Authorize]
public sealed class NotificationsHub : Hub
{
    public const string TenantIdClaim = "tenant_id";

    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.FindFirst(TenantIdClaim)?.Value;
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new HubException("Missing tenant_id.");

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(tenantId));
        await base.OnConnectedAsync();
    }

    public static string GroupName(Guid tenantId) => GroupName(tenantId.ToString());

    public static string GroupName(string tenantId) => $"tenant:{tenantId}";
}
