using Microsoft.AspNetCore.SignalR;
using Notification.Application.Abstractions;

namespace Notification.Api.Realtime;

public sealed class SignalRRealtimeNotifier : IRealtimeNotifier
{
    public const string ClientMethod = "notification";

    private readonly IHubContext<NotificationsHub> _hub;

    public SignalRRealtimeNotifier(IHubContext<NotificationsHub> hub)
    {
        _hub = hub;
    }

    public Task Publish(RealtimeNotification message, CancellationToken cancellationToken = default) =>
        _hub.Clients
            .Group(NotificationsHub.GroupName(message.TenantId))
            .SendAsync(ClientMethod, message, cancellationToken);
}
