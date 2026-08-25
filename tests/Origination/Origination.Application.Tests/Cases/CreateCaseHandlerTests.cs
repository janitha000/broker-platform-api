using Origination.Application.Abstractions;
using Origination.Application.Cases.CreateCase;
using Origination.Application.Cases.GetCase;
using Origination.Domain.Cases;

namespace Origination.Application.Tests.Cases;

public sealed class CreateCaseHandlerTests
{
    [Fact]
    public async Task Handle_NewCase_HasInquiryStatusAndTenant()
    {
        var repository = new InMemoryCaseRepository();
        var brokerId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var handler = new CreateCaseHandler(repository, new StubCurrentBroker(brokerId, tenantId));

        var result = await handler.Handle(new CreateCaseCommand("First home inquiry"));

        Assert.Equal(CaseStatus.Inquiry, result.Status);
        Assert.NotEqual(Guid.Empty, result.CaseId);

        var stored = await repository.GetById(result.CaseId, tenantId);
        Assert.NotNull(stored);
        Assert.Equal(brokerId, stored!.BrokerId);
        Assert.Equal(tenantId, stored.TenantId);
        Assert.Equal("First home inquiry", stored.InquiryNotes);
    }
}

public sealed class GetCaseHandlerTests
{
    [Fact]
    public async Task Handle_OtherTenant_ReturnsNull()
    {
        var repository = new InMemoryCaseRepository();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var created = await new CreateCaseHandler(repository, new StubCurrentBroker(Guid.NewGuid(), tenantA))
            .Handle(new CreateCaseCommand("notes"));

        var result = await new GetCaseHandler(repository, new StubCurrentBroker(Guid.NewGuid(), tenantB))
            .Handle(new GetCaseQuery(created.CaseId));

        Assert.Null(result);
    }
}

file sealed class StubCurrentBroker(Guid brokerId, Guid tenantId) : ICurrentBroker
{
    public Guid BrokerId { get; } = brokerId;
    public Guid TenantId { get; } = tenantId;
}

file sealed class InMemoryCaseRepository : ICaseRepository
{
    private readonly Dictionary<Guid, Case> _cases = new();

    public Task Add(Case @case, CancellationToken cancellationToken = default)
    {
        _cases[@case.Id] = @case;
        return Task.CompletedTask;
    }

    public Task<Case?> GetById(Guid caseId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        _cases.TryGetValue(caseId, out var @case);
        if (@case is null || @case.TenantId != tenantId)
            return Task.FromResult<Case?>(null);
        return Task.FromResult<Case?>(@case);
    }

    public Task Update(Case @case, CancellationToken cancellationToken = default)
    {
        _cases[@case.Id] = @case;
        return Task.CompletedTask;
    }
}
