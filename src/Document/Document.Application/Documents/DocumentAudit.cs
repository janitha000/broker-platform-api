using System.Text.Json;
using Broker.Hosting.Audit;
using Document.Domain.Documents;

namespace Document.Application.Documents;

public static class DocumentAudit
{
    public static AuditEvent For(
        CaseDocument document,
        Guid? brokerId,
        string action,
        string outcome = AuditOutcomes.Allow,
        string? actorType = null,
        string? detail = null) =>
        new()
        {
            EventId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            TenantId = document.TenantId,
            Action = action,
            Outcome = outcome,
            Actor = new AuditActor
            {
                Type = actorType ?? AuditActorTypes.User,
                BrokerId = brokerId,
            },
            Resource = new AuditResource
            {
                Type = AuditResourceTypes.Document,
                Id = document.Id.ToString("D"),
                CaseId = document.CaseId,
                Sensitivity = document.Sensitivity.ToString(),
            },
            DataJson = detail is null ? null : JsonSerializer.Serialize(new { detail }),
        };
}
