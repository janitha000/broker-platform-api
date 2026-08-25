using Origination.Application.Cases.CompleteFactFind;
using Origination.Domain.Cases;

namespace Origination.Application.Tests.Cases;

public sealed class CompleteFactFindHandlerTests
{
    [Fact]
    public async Task Handle_ExistingCase_SetsFactFindAndStatus()
    {
        var caseId = Guid.NewGuid();
        var repository = new InMemoryFactFindCaseRepository();
        await repository.Add(new Case
        {
            Id = caseId,
            BrokerId = Guid.NewGuid(),
            Status = CaseStatus.Inquiry,
            CreatedAt = DateTime.UtcNow
        });

        var handler = new CompleteFactFindHandler(repository);
        var result = await handler.Handle(new CompleteFactFindCommand(
            caseId,
            "Buy first home",
            120_000m,
            40_000m,
            80_000m,
            15_000m));

        Assert.NotNull(result);
        Assert.Equal(CaseStatus.FactFindCompleted, result!.Status);

        var stored = await repository.GetById(caseId);
        Assert.NotNull(stored!.FactFind);
        Assert.Equal("Buy first home", stored.FactFind.Objectives);
        Assert.Equal(120_000m, stored.FactFind.Income);
        Assert.Equal(40_000m, stored.FactFind.Expenses);
        Assert.Equal(80_000m, stored.FactFind.Assets);
        Assert.Equal(15_000m, stored.FactFind.Debts);
    }

    [Fact]
    public async Task Handle_MissingCase_ReturnsNull()
    {
        var handler = new CompleteFactFindHandler(new InMemoryFactFindCaseRepository());

        var result = await handler.Handle(new CompleteFactFindCommand(
            Guid.NewGuid(),
            "x",
            1m, 1m, 1m, 1m));

        Assert.Null(result);
    }
}

file sealed class InMemoryFactFindCaseRepository : ICaseRepository
{
    private readonly Dictionary<Guid, Case> _cases = new();

    public Task Add(Case @case, CancellationToken cancellationToken = default)
    {
        _cases[@case.Id] = @case;
        return Task.CompletedTask;
    }

    public Task Update(Case @case, CancellationToken cancellationToken = default)
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