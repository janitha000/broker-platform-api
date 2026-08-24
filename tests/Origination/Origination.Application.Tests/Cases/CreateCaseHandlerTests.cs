using Origination.Application.Abstractions;
using Origination.Application.Cases.CreateCase;
using Origination.Domain.Cases;

namespace Origination.Application.Tests.Cases;

public sealed class CreateCaseHandlerTests
{
    [Fact]
    public async Task Handle_NewCase_HasInquiryStatus()
    {
        var repository = new InMemoryCaseRepository();
        var brokerId = Guid.NewGuid();
        var handler = new CreateCaseHandler(repository, new StubCurrentBroker(brokerId));

        var result = await handler.Handle(new CreateCaseCommand("First home inquiry"));

        Assert.Equal(CaseStatus.Inquiry, result.Status);
        Assert.NotEqual(Guid.Empty, result.CaseId);

        var stored = await repository.GetById(result.CaseId);
        Assert.NotNull(stored);
        Assert.Equal(brokerId, stored!.BrokerId);
        Assert.Equal("First home inquiry", stored.InquiryNotes);
    }
}

file sealed class StubCurrentBroker(Guid brokerId) : ICurrentBroker
{
    public Guid BrokerId { get; } = brokerId;
}

file sealed class InMemoryCaseRepository : ICaseRepository
{
    private readonly Dictionary<Guid, Case> _cases = new();

    public Task Add(Case @case, CancellationToken cancellationToken = default)
    {
        _cases[@case.Id] = @case;
        return Task.CompletedTask;
    }

    public Task<Case?> GetById(Guid caseId, CancellationToken cancellationToken = default)
    {
        _cases.TryGetValue(caseId, out var @case);
        return Task.FromResult(@case);
    }
}