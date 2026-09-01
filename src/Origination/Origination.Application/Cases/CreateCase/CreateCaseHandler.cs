using Origination.Application.Abstractions;
using Origination.Domain.Abstractions;
using Origination.Domain.Cases;

namespace Origination.Application.Cases.CreateCase;

public sealed class CreateCaseHandler
{
    private readonly ICaseRepository _caseRepository;
    private readonly ICurrentBroker _currentBroker;

    private readonly IUnitOfWork _unitOfWork;

    public CreateCaseHandler(ICaseRepository caseRepository, ICurrentBroker currentBroker, IUnitOfWork unitOfWork)
    {
        _caseRepository = caseRepository;
        _currentBroker = currentBroker;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateCaseResult> Handle(CreateCaseCommand command, CancellationToken cancellationToken = default)
    {
        var @case = new Case
        {
            Id = Guid.NewGuid(),
            BrokerId = _currentBroker.BrokerId,
            TenantId = _currentBroker.TenantId,
            InquiryNotes = command.InquiryNotes ?? string.Empty,
            Status = CaseStatus.Inquiry,
            CreatedAt = DateTime.UtcNow,
        };

        await _caseRepository.Add(@case, cancellationToken);
        await _unitOfWork.SaveChanges(cancellationToken);

        return new CreateCaseResult(@case.Id, @case.Status);
    }
}