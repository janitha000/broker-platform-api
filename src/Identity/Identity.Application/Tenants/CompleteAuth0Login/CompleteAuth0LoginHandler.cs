using Identity.Application.Abstractions;
using Identity.Application.Tenants.Login;
using Identity.Domain.Tenants;

namespace Identity.Application.Tenants.CompleteAuth0Login;

public sealed class CompleteAuth0LoginHandler
{
    private readonly IBrokerUserRepository _brokerUserRepository;
    private readonly ITokenIssuer _tokenIssuer;

    public CompleteAuth0LoginHandler(
        IBrokerUserRepository brokerUserRepository,
        ITokenIssuer tokenIssuer)
    {
        _brokerUserRepository = brokerUserRepository;
        _tokenIssuer = tokenIssuer;
    }

    public async Task<LoginResult?> Handle(
        CompleteAuth0LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
            return null;

        var email = command.Email.Trim().ToLowerInvariant();
        var user = await _brokerUserRepository.GetByEmail(email, cancellationToken);
        if (user is null)
            return null;

        var token = _tokenIssuer.Issue(user.Id, user.TenantId, user.Email);
        return new LoginResult(user.TenantId, user.Id, user.Email, token);
    }
}
