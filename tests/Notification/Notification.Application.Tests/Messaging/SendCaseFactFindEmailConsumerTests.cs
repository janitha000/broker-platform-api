using Broker.Contracts;
using Broker.Contracts.Notification;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notification.Application.Abstractions;
using Notification.Application.Notifications.SendNotification;
using Notification.Application.Templating;
using Notification.Application.Tests.Notifications;
using Notification.Domain.Notifications;
using Notification.Infrastructure.Messaging;

namespace Notification.Application.Tests.Messaging;

public sealed class SendCaseFactFindEmailConsumerTests
{
    [Fact]
    public async Task Consume_Command_SendsEmail()
    {
        var store = new InMemoryStore();
        store.SeedTemplate();
        var email = new CountingEmail(true);

        await using var provider = BuildProvider(store, email);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            await SendCommand(harness, SampleMessage("origination:ok:email"));

            Assert.True(await harness.Consumed.Any<SendCaseFactFindEmail>());
            Assert.Equal(1, email.Calls);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task Consume_EmailFailsThenSucceeds_RetriesAndSends()
    {
        var store = new InMemoryStore();
        store.SeedTemplate();
        var email = new FailThenSucceedEmail(failTimes: 2);

        await using var provider = BuildProvider(store, email);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            await SendCommand(harness, SampleMessage("origination:retry:email"));

            Assert.True(await harness.Consumed.Any<SendCaseFactFindEmail>(
                x => x.Exception is null),
                "consumer should succeed after email retries");
            Assert.Equal(3, email.Calls);
            Assert.False(await harness.Published.Any<Fault<SendCaseFactFindEmail>>());
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task Consume_EmailAlwaysFails_RetriesThenFaults()
    {
        var store = new InMemoryStore();
        store.SeedTemplate();
        var email = new CountingEmail(succeeds: false);

        await using var provider = BuildProvider(store, email);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            await SendCommand(harness, SampleMessage("origination:dead:email"));

            Assert.True(await harness.Published.Any<Fault<SendCaseFactFindEmail>>());
            Assert.Equal(1 + NotificationMessageRetry.EmailImmediateRetries, email.Calls);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task Consume_UnknownTemplate_FaultsWithoutRetry()
    {
        var store = new InMemoryStore();
        var email = new CountingEmail(true);

        await using var provider = BuildProvider(store, email);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            await SendCommand(harness, SampleMessage("origination:notemplate:email"));

            Assert.True(await harness.Published.Any<Fault<SendCaseFactFindEmail>>());
            Assert.Equal(0, email.Calls);
            var consumed = harness.GetConsumerHarness<SendCaseFactFindEmailConsumer>()
                .Consumed.Select<SendCaseFactFindEmail>().ToList();
            Assert.Single(consumed);
        }
        finally
        {
            await harness.Stop();
        }
    }

    private static ServiceProvider BuildProvider(InMemoryStore store, IEmailProvider email) =>
        new ServiceCollection()
            .AddSingleton<INotificationRepository>(store)
            .AddSingleton<INotificationTemplateRepository>(store)
            .AddSingleton<ITemplateRenderer, PlaceholderTemplateRenderer>()
            .AddSingleton(email)
            .AddScoped<SendNotificationHandler>()
            .AddMassTransitTestHarness(bus => bus.AddSendCaseFactFindEmailConsumer())
            .BuildServiceProvider(true);

    private static async Task SendCommand(ITestHarness harness, SendCaseFactFindEmail message)
    {
        var endpoint = await harness.Bus.GetSendEndpoint(
            new Uri($"queue:{BrokerCommandQueues.SendCaseFactFindEmail}"));
        await endpoint.Send(message);
    }

    private static SendCaseFactFindEmail SampleMessage(string idempotencyKey)
    {
        var caseId = Guid.NewGuid();
        return new SendCaseFactFindEmail
        {
            CaseId = caseId,
            TenantId = Guid.NewGuid(),
            BrokerId = Guid.NewGuid(),
            IdempotencyKey = idempotencyKey,
            CorrelationId = caseId.ToString(),
            Recipient = "broker@example.com",
            Data = new Dictionary<string, string>
            {
                ["caseId"] = caseId.ToString(),
                ["brokerName"] = "Ada",
            },
        };
    }
}

sealed class FailThenSucceedEmail(int failTimes) : IEmailProvider
{
    public int Calls { get; private set; }

    public Task<EmailSendResult> Send(EmailMessage message, CancellationToken cancellationToken = default)
    {
        Calls++;
        var ok = Calls > failTimes;
        return Task.FromResult(ok
            ? new EmailSendResult(true, "msg-1", null, null)
            : new EmailSendResult(false, null, "Decline", "failed"));
    }
}
