using Broker.Contracts.Origination;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Origination.Infrastructure.Messaging;
using Origination.Infrastructure.Messaging.Sagas;

namespace Origination.Application.Tests.Messaging;

public sealed class CaseLifecycleStateMachineTests
{
    [Fact]
    public async Task CaseOpened_CreatesSagaInEnquiry()
    {
        await using var provider = BuildProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            var caseId = Guid.NewGuid();
            await harness.Bus.Publish(Opened(caseId));

            var saga = harness.GetSagaStateMachineHarness<CaseLifecycleStateMachine, CaseLifecycleState>();
            Assert.True(await saga.Created.Any(x => x.Saga.CorrelationId == caseId));
            var instance = saga.Created.Contains(caseId);
            Assert.Equal(nameof(CaseLifecycleStateMachine.Enquiry), instance?.CurrentState);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task FactFindCompleted_MovesEnquiryToFactFindCompleted()
    {
        await using var provider = BuildProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            var caseId = Guid.NewGuid();
            await harness.Bus.Publish(Opened(caseId));

            var saga = harness.GetSagaStateMachineHarness<CaseLifecycleStateMachine, CaseLifecycleState>();
            Assert.True(await saga.Created.Any(x => x.Saga.CorrelationId == caseId));

            await harness.Bus.Publish(FactFind(caseId));

            Assert.True(await saga.Consumed.Any<CaseFactFindCompleted>());
            Assert.True(await harness.Sent.Any<Broker.Contracts.Notification.SendCaseFactFindEmail>(
                x => x.Context.Message.CaseId == caseId));
            var instance = saga.Sagas.Contains(caseId);
            Assert.Equal(nameof(CaseLifecycleStateMachine.FactFindCompleted), instance?.CurrentState);
            Assert.NotNull(instance?.FactFindCompletedAt);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task DuplicateCaseOpened_DoesNotCreateSecondInstance()
    {
        await using var provider = BuildProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            var caseId = Guid.NewGuid();
            var opened = Opened(caseId);
            await harness.Bus.Publish(opened);
            var saga = harness.GetSagaStateMachineHarness<CaseLifecycleStateMachine, CaseLifecycleState>();
            Assert.True(await saga.Created.Any(x => x.Saga.CorrelationId == caseId));

            await harness.Bus.Publish(opened);

            Assert.True(await saga.Consumed.Any<CaseOpened>());
            var instance = saga.Sagas.Contains(caseId);
            Assert.NotNull(instance);
            Assert.Equal(nameof(CaseLifecycleStateMachine.Enquiry), instance!.CurrentState);
            Assert.Null(instance.FactFindCompletedAt);
            Assert.Empty(harness.Sent.Select<Broker.Contracts.Notification.SendCaseFactFindEmail>());
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task FactFindCompleted_WithoutCaseOpened_DoesNotCreateSaga()
    {
        await using var provider = BuildProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            var caseId = Guid.NewGuid();
            await harness.Bus.Publish(FactFind(caseId));

            Assert.True(await harness.Consumed.Any<CaseFactFindCompleted>());
            var saga = harness.GetSagaStateMachineHarness<CaseLifecycleStateMachine, CaseLifecycleState>();
            Assert.Null(saga.Created.Contains(caseId));
            Assert.Empty(harness.Sent.Select<Broker.Contracts.Notification.SendCaseFactFindEmail>());
        }
        finally
        {
            await harness.Stop();
        }
    }

    private static ServiceProvider BuildProvider() =>
        new ServiceCollection()
            .AddMassTransitTestHarness(bus => bus.AddCaseLifecycleSaga(persistToSql: false))
            .BuildServiceProvider(true);

    private static CaseOpened Opened(Guid caseId) =>
        new()
        {
            CaseId = caseId,
            TenantId = Guid.NewGuid(),
            BrokerId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
        };

    private static CaseFactFindCompleted FactFind(Guid caseId) =>
        new()
        {
            CaseId = caseId,
            TenantId = Guid.NewGuid(),
            BrokerId = Guid.NewGuid(),
            IdempotencyKey = $"origination:{caseId}:fact-find-completed:email",
            CorrelationId = caseId.ToString(),
        };
}
