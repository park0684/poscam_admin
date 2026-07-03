using poscam.AdminWeb.Models.Account;
using poscam.AdminWeb.Services;
using Xunit;

namespace poscam.AdminWeb.Tests;

public class MenuAccessFilterTests
{
    [Fact]
    public void Filter_PartnerUserWithStorePermission_ShowsStoreMenu()
    {
        var filter = new MenuAccessFilter();
        var access = CreateAccess(
            CurrentUserAccessPolicy.PartnerUserRole,
            CurrentUserAccessPolicy.StoreManagePermissionCode);

        var result = filter.Filter(MenuConfiguration.GetMenus(), access);

        var storeMenu = Assert.Single(
            result,
            menu => menu.Key == "stores");
        Assert.Single(storeMenu.Children);
        Assert.Equal("store-list", storeMenu.Children[0].Key);
    }

    [Fact]
    public void Filter_PartnerUserWithoutStorePermission_HidesStoreMenu()
    {
        var filter = new MenuAccessFilter();
        var access = CreateAccess(
            CurrentUserAccessPolicy.PartnerUserRole);

        var result = filter.Filter(MenuConfiguration.GetMenus(), access);

        Assert.DoesNotContain(result, menu => menu.Key == "stores");
    }

    [Fact]
    public void Filter_AdminWithStorePermission_ShowsStoreMenu()
    {
        var filter = new MenuAccessFilter();
        var access = CreateAccess(
            CurrentUserAccessPolicy.AdminRole,
            CurrentUserAccessPolicy.StoreManagePermissionCode);

        var result = filter.Filter(MenuConfiguration.GetMenus(), access);

        Assert.Contains(result, menu => menu.Key == "stores");
    }

    [Fact]
    public void Filter_SystemWithoutPermissionCodes_ShowsStoreMenu()
    {
        var filter = new MenuAccessFilter();
        var access = CreateAccess(CurrentUserAccessPolicy.SystemRole);

        var result = filter.Filter(MenuConfiguration.GetMenus(), access);

        Assert.Contains(result, menu => menu.Key == "stores");
    }

    [Fact]
    public void Filter_PartnerUserWithManagePermission_ShowsUserMenu()
    {
        var filter = new MenuAccessFilter();
        var access = CreateAccess(
            CurrentUserAccessPolicy.PartnerUserRole,
            CurrentUserAccessPolicy.PartnerUserManagePermissionCode);

        var result = filter.Filter(MenuConfiguration.GetMenus(), access);

        var partnerMenu = Assert.Single(
            result,
            menu => menu.Key == "partners");
        Assert.Contains(
            partnerMenu.Children,
            menu => menu.Key == "user-list");
    }

    [Fact]
    public void Filter_PartnerUserWithoutManagePermission_HidesUserMenu()
    {
        var filter = new MenuAccessFilter();
        var access = CreateAccess(CurrentUserAccessPolicy.PartnerUserRole);

        var result = filter.Filter(MenuConfiguration.GetMenus(), access);

        var partnerMenu = Assert.Single(
            result,
            menu => menu.Key == "partners");
        Assert.DoesNotContain(
            partnerMenu.Children,
            menu => menu.Key == "user-list");
    }

    [Fact]
    public void MenuConfiguration_DoesNotContainStandalonePartnerPermissionMenu()
    {
        var partnerMenu = Assert.Single(
            MenuConfiguration.GetMenus(),
            menu => menu.Key == "partners");

        Assert.DoesNotContain(
            partnerMenu.Children,
            menu => menu.Key == "partner-user-permissions");
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
