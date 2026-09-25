using Broker.Contracts.Origination;
using Origination.Application.Abstractions;
using Origination.Domain.Abstractions;
using Origination.Domain.Cases;

namespace Origination.Application.Cases.CreateCase;

public sealed class CreateCaseHandler
{
    private readonly ICaseRepository _caseRepository;
    private readonly ICurrentBroker _currentBroker;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICaseOpenedPublisher _caseOpened;

    public CreateCaseHandler(
        ICaseRepository caseRepository,
        ICurrentBroker currentBroker,
        IUnitOfWork unitOfWork,
        ICaseOpenedPublisher caseOpened)
    {
        _caseRepository = caseRepository;
        _currentBroker = currentBroker;
        _unitOfWork = unitOfWork;
        _caseOpened = caseOpened;
    }

    public async Task<CreateCaseResult> Handle(CreateCaseCommand command, CancellationToken cancellationToken = default)
    {
        var @case = new Case
        {
            Id = Guid.NewGuid(),
            BrokerId = _currentBroker.BrokerId,
            TenantId = _currentBroker.TenantId,
            InquiryNotes = command.InquiryNotes ?? string.Empty,
            Status = CaseStatus.Enquiry,
            CreatedAt = DateTime.UtcNow,
        };

        await _caseRepository.Add(@case, cancellationToken);

        if (_caseOpened.UsesBusOutbox)
        {
            await _caseOpened.Publish(
                new CaseOpened
                {
                    CaseId = @case.Id,
                    TenantId = @case.TenantId,
                    BrokerId = @case.BrokerId,
                    OccurredAt = @case.CreatedAt,
                },
                cancellationToken);
        }

        await _unitOfWork.SaveChanges(cancellationToken);

        return new CreateCaseResult(@case.Id, @case.Status);
    }
}