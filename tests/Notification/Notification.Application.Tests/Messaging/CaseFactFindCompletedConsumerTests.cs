using Broker.Contracts.Origination;
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

public sealed class CaseFactFindCompletedConsumerTests
{
    [Fact]
    public async Task Consume_PublishesFactFindCompleted_SendsEmail()
    {
        var store = new InMemoryStore();
        store.SeedTemplate();
        var email = new CountingEmail(true);

        await using var provider = new ServiceCollection()
            .AddSingleton<INotificationRepository>(store)
            .AddSingleton<INotificationTemplateRepository>(store)
            .AddSingleton<ITemplateRenderer, PlaceholderTemplateRenderer>()
            .AddSingleton<IEmailProvider>(email)
            .AddScoped<SendNotificationHandler>()
            .AddMassTransitTestHarness(bus =>
            {
                bus.AddConsumer<CaseFactFindCompletedConsumer>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            var caseId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee1");
            await harness.Bus.Publish(new CaseFactFindCompleted
            {
                CaseId = caseId,
                TenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee2"),
                BrokerId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee3"),
                IdempotencyKey = "origination:case:fact-find-completed:email",
                CorrelationId = caseId.ToString(),
                Recipient = "broker@example.com",
                Data = new Dictionary<string, string>
                {
                    ["caseId"] = caseId.ToString(),
                    ["brokerName"] = "Ada",
                },
            });

            Assert.True(await harness.Consumed.Any<CaseFactFindCompleted>());
            Assert.True(await harness.GetConsumerHarness<CaseFactFindCompletedConsumer>()
                .Consumed.Any<CaseFactFindCompleted>());
            Assert.Equal(1, email.Calls);
        }
        finally
        {
            await harness.Stop();
        }
    }
}
