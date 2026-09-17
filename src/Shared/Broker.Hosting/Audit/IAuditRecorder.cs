namespace Broker.Hosting.Audit;

public interface IAuditRecorder
{
    void Record(AuditEvent auditEvent);
    Task Flush(CancellationToken cancellationToken = default);
}