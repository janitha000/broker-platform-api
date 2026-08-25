using Identity.Application.Abstractions;
using Identity.Domain.Tenants;

namespace Identity.Application.Tenants.Login;

public sealed class LoginHandler
{
    private readonly IBrokerUserRepository _brokerUserRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenIssuer _tokenIssuer;

    public LoginHandler(
        IBrokerUserRepository brokerUserRepository,
        IPasswordHasher passwordHasher,
        ITokenIssuer tokenIssuer)
    {
        _brokerUserRepository = brokerUserRepository;
        _passwordHasher = passwordHasher;
        _tokenIssuer = tokenIssuer;
    }

    public async Task<LoginResult?> Handle(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var email = command.Email.Trim().ToLowerInvariant();
        var brokerUser = await _brokerUserRepository.GetByEmail(email, cancellationToken);
        if (brokerUser is null)
            return null;

        if (!_passwordHasher.Verify(brokerUser.PasswordHash, command.Password))
            return null;

        var accessToken = _tokenIssuer.Issue(brokerUser.Id, brokerUser.TenantId, brokerUser.Email);

        return new LoginResult(brokerUser.TenantId, brokerUser.Id, brokerUser.Email, accessToken);
    }
}
