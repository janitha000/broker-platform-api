using Notification.Application.Abstractions;
using Notification.Application.Notifications.SendNotification;
using Notification.Domain.Notifications;
using NotificationEntity = Notification.Domain.Notifications.Notification;

namespace Notification.Application.Tests.Notifications;

public sealed class SendNotificationHandlerTests
{
    private static SendNotificationCommand Command(
        string key = "key-1",
        string recipient = "broker@example.com",
        string body = "Fact-find completed.") =>
        new(
            NotificationChannel.Email,
            recipient,
            "Case update",
            body,
            "origination",
            key,
            "case-1");

    [Fact]
    public async Task Handle_SenderSucceeds_PersistsSent()
    {
        var repo = new InMemoryNotificationRepository();
        var handler = new SendNotificationHandler(repo, new StubSender(succeeds: true));

        var outcome = await handler.Handle(Command());

        Assert.Equal(SendNotificationKind.Sent, outcome.Kind);
        Assert.Equal(NotificationStatus.Sent, outcome.Notification!.Status);
        var stored = await repo.GetByIdempotencyKey("key-1");
        Assert.NotNull(stored);
        Assert.NotNull(stored.SentAt);
    }

    [Fact]
    public async Task Handle_SenderFails_PersistsFailed()
    {
        var repo = new InMemoryNotificationRepository();
        var handler = new SendNotificationHandler(repo, new StubSender(succeeds: false));

        var outcome = await handler.Handle(Command(recipient: "fail@example.com"));

        Assert.Equal(SendNotificationKind.Failed, outcome.Kind);
        Assert.Equal(NotificationStatus.Failed, outcome.Notification!.Status);
    }

    [Fact]
    public async Task Handle_SameKeySamePayload_ReplaysWithoutSecondSend()
    {
        var repo = new InMemoryNotificationRepository();
        var sender = new CountingSender(succeeds: true);
        var handler = new SendNotificationHandler(repo, sender);

        var first = await handler.Handle(Command());
        var second = await handler.Handle(Command());

        Assert.Equal(first.Notification!.NotificationId, second.Notification!.NotificationId);
        Assert.Equal(1, sender.Calls);
    }

    [Fact]
    public async Task Handle_SameKeyDifferentBody_IsConflict()
    {
        var repo = new InMemoryNotificationRepository();
        var handler = new SendNotificationHandler(repo, new StubSender(true));

        await handler.Handle(Command(body: "first"));
        var second = await handler.Handle(Command(body: "second"));

        Assert.Equal(SendNotificationKind.IdempotencyConflict, second.Kind);
    }
}

file sealed class StubSender(bool succeeds) : INotificationSender
{
    public Task<bool> Send(
        string channel,
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(succeeds);
}

file sealed class CountingSender(bool succeeds) : INotificationSender
{
    public int Calls { get; private set; }

    public Task<bool> Send(
        string channel,
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        return Task.FromResult(succeeds);
    }
}

file sealed class InMemoryNotificationRepository : INotificationRepository
{
    private readonly Dictionary<string, NotificationEntity> _byKey = new();
    private readonly Dictionary<Guid, NotificationEntity> _byId = new();

    public Task<NotificationEntity?> GetByIdempotencyKey(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        _byKey.TryGetValue(idempotencyKey, out var notification);
        return Task.FromResult(notification);
    }

    public Task<NotificationEntity?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        _byId.TryGetValue(id, out var notification);
        return Task.FromResult(notification);
    }

    public Task<NotificationEntity> Add(
        NotificationEntity notification,
        CancellationToken cancellationToken = default)
    {
        _byKey.Add(notification.IdempotencyKey, notification);
        _byId.Add(notification.Id, notification);
        return Task.FromResult(notification);
    }
}
