using poscam.AdminWeb.Models.Account;
using poscam.AdminWeb.Services;
using Xunit;

namespace poscam.AdminWeb.Tests;

public class CurrentUserAccessPolicyTests
{
    [Fact]
    public void CanResetPartnerUserPassword_PartnerUserWithPermission_ReturnsTrue()
    {
        var access = CreateAccess(
            CurrentUserAccessPolicy.PartnerUserRole,
            CurrentUserAccessPolicy.PartnerUserPasswordResetPermissionCode);

        Assert.True(
            CurrentUserAccessPolicy.CanResetPartnerUserPassword(access));
    }

    [Fact]
    public void CanResetPartnerUserPassword_PartnerUserWithoutPermission_ReturnsFalse()
    {
        var access = CreateAccess(CurrentUserAccessPolicy.PartnerUserRole);

        Assert.False(
            CurrentUserAccessPolicy.CanResetPartnerUserPassword(access));
    }

    [Fact]
    public void CanResetPartnerUserPassword_AdminRequiresPermission()
    {
        var allowed = CreateAccess(
            CurrentUserAccessPolicy.AdminRole,
            CurrentUserAccessPolicy.PartnerUserPasswordResetPermissionCode);
        var denied = CreateAccess(CurrentUserAccessPolicy.AdminRole);

        Assert.True(
            CurrentUserAccessPolicy.CanResetPartnerUserPassword(allowed));
        Assert.False(
            CurrentUserAccessPolicy.CanResetPartnerUserPassword(denied));
    }

    [Fact]
    public void CanResetPartnerUserPassword_SystemIsAlwaysAllowed()
    {
        var access = CreateAccess(CurrentUserAccessPolicy.SystemRole);

        Assert.True(
            CurrentUserAccessPolicy.CanResetPartnerUserPassword(access));
    }

    private static CurrentUserAccessResponse CreateAccess(
        int role,
        params int[] permissionCodes)
    {
        return new CurrentUserAccessResponse
        {
            UserCode = 1,
            UserName = "테스트 사용자",
            UserRole = role,
            PermissionCodes = permissionCodes.ToList()
        };
    }
}
