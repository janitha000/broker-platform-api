using Notification.Application.Abstractions;
using Notification.Application.Notifications.SendNotification;
using Notification.Application.Templating;
using Notification.Domain.Notifications;
using NotificationEntity = Notification.Domain.Notifications.Notification;

namespace Notification.Application.Tests.Notifications;

public sealed class SendNotificationHandlerTests
{
    private static SendNotificationCommand Command(
        string key = "key-1",
        string recipient = "broker@example.com",
        string templateKey = "case.fact-find-completed",
        string? caseId = "case-1") =>
        new(
            NotificationChannel.Email,
            recipient,
            templateKey,
            new Dictionary<string, string> { ["caseId"] = caseId!, ["brokerName"] = "Ada" },
            "origination",
            key,
            "case-1");

    private static SendNotificationHandler Handler(InMemoryStore store, bool sendSucceeds = true) =>
        new(store, store, new PlaceholderTemplateRenderer(), new StubEmail(sendSucceeds));

    [Fact]
    public async Task Handle_Succeeds_PersistsSentAndAttempt()
    {
        var store = new InMemoryStore();
        store.SeedTemplate();

        var outcome = await Handler(store).Handle(Command());

        Assert.Equal(SendNotificationKind.Sent, outcome.Kind);
        var stored = await store.GetByIdempotencyKey("key-1");
        Assert.Equal(NotificationStatus.Sent, stored!.Status);
        Assert.Contains("case-1", stored.RenderedSubject);
        Assert.Single(stored.Attempts);
        Assert.True(stored.Attempts.First().Succeeded);
    }

    [Fact]
    public async Task Handle_ProviderFails_PersistsFailedAttempt()
    {
        var store = new InMemoryStore();
        store.SeedTemplate();

        var outcome = await Handler(store, sendSucceeds: false).Handle(Command());

        Assert.Equal(SendNotificationKind.Failed, outcome.Kind);
        var stored = await store.GetByIdempotencyKey("key-1");
        Assert.Equal(NotificationStatus.Failed, stored!.Status);
        Assert.False(stored.Attempts.First().Succeeded);
    }

    [Fact]
    public async Task Handle_MissingTemplate_IsNotFound()
    {
        var store = new InMemoryStore();

        var outcome = await Handler(store).Handle(Command());

        Assert.Equal(SendNotificationKind.TemplateNotFound, outcome.Kind);
    }

    [Fact]
    public async Task Handle_SameKeySamePayload_ReplaysWithoutSecondSend()
    {
        var store = new InMemoryStore();
        store.SeedTemplate();
        var email = new CountingEmail(true);
        var handler = new SendNotificationHandler(store, store, new PlaceholderTemplateRenderer(), email);

        var first = await handler.Handle(Command());
        var second = await handler.Handle(Command());

        Assert.Equal(first.Notification!.NotificationId, second.Notification!.NotificationId);
        Assert.Equal(1, email.Calls);
    }

    [Fact]
    public async Task Handle_SameKeyDifferentData_IsConflict()
    {
        var store = new InMemoryStore();
        store.SeedTemplate();
        var handler = Handler(store);

        await handler.Handle(Command(caseId: "a"));
        var second = await handler.Handle(Command(caseId: "b"));

        Assert.Equal(SendNotificationKind.IdempotencyConflict, second.Kind);
    }

    [Fact]
    public async Task Handle_AcceptedRow_ResumesSend()
    {
        var store = new InMemoryStore();
        store.SeedTemplate();
        var existing = new NotificationEntity
        {
            Id = Guid.NewGuid(),
            Channel = NotificationChannel.Email,
            Recipient = "broker@example.com",
            TemplateKey = "case.fact-find-completed",
            TemplateData = "brokerName=Ada&caseId=case-1",
            RenderedSubject = "Fact-find completed for case case-1",
            RenderedBody = "Hi Ada, fact-find is complete for case case-1.",
            Source = "origination",
            CorrelationId = "case-1",
            Status = NotificationStatus.Accepted,
            IdempotencyKey = "key-1",
            PayloadFingerprint = "Email|broker@example.com|case.fact-find-completed|brokerName=Ada&caseId=case-1|origination|case-1",
            CreatedAt = DateTime.UtcNow,
        };
        existing.Attempts.Add(new DeliveryAttempt
        {
            Id = Guid.NewGuid(),
            NotificationId = existing.Id,
            StartedAt = DateTime.UtcNow,
        });
        await store.Add(existing);

        var outcome = await Handler(store).Handle(Command());

        Assert.Equal(SendNotificationKind.Sent, outcome.Kind);
        var stored = await store.GetByIdempotencyKey("key-1");
        Assert.Equal(NotificationStatus.Sent, stored!.Status);
        Assert.True(stored.Attempts.First().Succeeded);
    }
}

sealed class StubEmail(bool succeeds) : IEmailProvider
{
    public Task<EmailSendResult> Send(EmailMessage message, CancellationToken cancellationToken = default) =>
        Task.FromResult(succeeds
            ? new EmailSendResult(true, "msg-1", null, null)
            : new EmailSendResult(false, null, "Decline", "failed"));
}

sealed class CountingEmail(bool succeeds) : IEmailProvider
{
    public int Calls { get; private set; }

    public Task<EmailSendResult> Send(EmailMessage message, CancellationToken cancellationToken = default)
    {
        Calls++;
        return Task.FromResult(new EmailSendResult(succeeds, "msg-1", null, null));
    }
}

sealed class InMemoryStore : INotificationRepository, INotificationTemplateRepository
{
    private readonly Dictionary<string, NotificationEntity> _byKey = new();
    private readonly List<NotificationTemplate> _templates = new();

    public void SeedTemplate()
    {
        _templates.Add(new NotificationTemplate
        {
            Id = Guid.NewGuid(),
            Key = "case.fact-find-completed",
            Channel = NotificationChannel.Email,
            Locale = "en-AU",
            Version = 1,
            SubjectTemplate = "Fact-find completed for case {{caseId}}",
            BodyTemplate = "Hi {{brokerName}}, fact-find is complete for case {{caseId}}.",
            IsActive = true,
        });
    }

    public Task<NotificationEntity?> GetByIdempotencyKey(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        _byKey.TryGetValue(idempotencyKey, out var n);
        return Task.FromResult(n);
    }

    public Task<NotificationEntity?> GetById(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byKey.Values.FirstOrDefault(n => n.Id == id));

    public Task<NotificationEntity> Add(NotificationEntity notification, CancellationToken cancellationToken = default)
    {
        if (_byKey.TryGetValue(notification.IdempotencyKey, out var existing))
            return Task.FromResult(existing);
        _byKey.Add(notification.IdempotencyKey, notification);
        return Task.FromResult(notification);
    }

    public Task Update(NotificationEntity notification, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<NotificationTemplate?> GetActive(string key, string channel, CancellationToken cancellationToken = default) =>
        Task.FromResult(_templates
            .Where(t => t.Key == key && t.Channel == channel && t.IsActive)
            .OrderByDescending(t => t.Version)
            .FirstOrDefault());
}
