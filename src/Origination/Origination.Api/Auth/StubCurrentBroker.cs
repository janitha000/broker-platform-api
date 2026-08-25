using Origination.Application.Abstractions;

namespace Origination.Api.Auth;

public sealed class StubCurrentBroker : ICurrentBroker
{
    public static readonly Guid DevBrokerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public Guid BrokerId => DevBrokerId;
}