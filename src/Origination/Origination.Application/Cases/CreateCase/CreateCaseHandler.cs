using Origination.Application.Abstractions;
using Origination.Domain.Cases;

namespace Origination.Application.Cases.CreateCase;

public sealed class CreateCaseHandler 
{
    private readonly ICaseRepository _caseRepository;
    private readonly ICurrentBroker _currentBroker;

    public CreateCaseHandler(ICaseRepository caseRepository, ICurrentBroker currentBroker)
    {
        _caseRepository = caseRepository;
        _currentBroker = currentBroker;
    }

    public async Task<CreateCaseResult> Handle(CreateCaseCommand command, CancellationToken cancellationToken = default)
    {
        var @case = new Case
        {
            Id = Guid.NewGuid(),
            BrokerId = _currentBroker.BrokerId,
            InquiryNotes = command.InquiryNotes ?? string.Empty,
            Status = CaseStatus.Inquiry,
            CreatedAt = DateTime.UtcNow,
        };

        await _caseRepository.Add(@case, cancellationToken);

        return new CreateCaseResult(@case.Id, @case.Status);
    }
}