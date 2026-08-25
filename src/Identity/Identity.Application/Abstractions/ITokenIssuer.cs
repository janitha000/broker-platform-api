namespace Identity.Application.Abstractions;

public interface ITokenIssuer
{
    string Issue(Guid brokerId, Guid tenantId, string email);
}
