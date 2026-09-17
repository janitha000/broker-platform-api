using Identity.Domain.Tenants;

namespace Identity.Application.Tests.Tenants;

public sealed class BrokerPermissionsTests
{
    [Fact]
    public void Principal_CanSettle_AssistantCannot()
    {
        Assert.Contains(BrokerPermissions.CasesSettle, BrokerPermissions.ForRole(BrokerRole.Principal));
        Assert.DoesNotContain(BrokerPermissions.CasesSettle, BrokerPermissions.ForRole(BrokerRole.Assistant));
        Assert.DoesNotContain(BrokerPermissions.CasesFactFindAny, BrokerPermissions.ForRole(BrokerRole.Assistant));
        Assert.Equal(
            [BrokerPermissions.CasesRead, BrokerPermissions.DocumentsRead],
            BrokerPermissions.ForRole(BrokerRole.ReadOnly));
        Assert.Contains(BrokerPermissions.DocumentsUpload, BrokerPermissions.ForRole(BrokerRole.Assistant));
        Assert.DoesNotContain(
            BrokerPermissions.DocumentsSensitiveRead,
            BrokerPermissions.ForRole(BrokerRole.Assistant));
        Assert.Contains(
            BrokerPermissions.DocumentsSensitiveRead,
            BrokerPermissions.ForRole(BrokerRole.Principal));
        Assert.Empty(BrokerPermissions.ForRole("Nope"));
    }
}
