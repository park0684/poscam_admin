using poscam.AdminWeb.Models.Account;
using poscam.AdminWeb.Models.Navigation;
using poscam.AdminWeb.Services;
using Xunit;

namespace poscam.AdminWeb.Tests.Navigation;

public class MenuAccessFilterTests
{
    private readonly MenuAccessFilter _filter = new();

    [Fact]
    public void Filter_System은_세부권한없이_업데이트메뉴를_표시한다()
    {
        var menus = Filter(CreateAccess(CurrentUserAccessPolicy.SystemRole));

        var updateMenu = Assert.Single(
            menus,
            menu => menu.Key == "updates");
        Assert.Equal(2, updateMenu.Children.Count);
        Assert.Contains(updateMenu.Children, child => child.Url == "updates/releases");
        Assert.Contains(updateMenu.Children, child => child.Url == "updates/audit-logs");
    }

    [Fact]
    public void Filter_Admin은_UpdateManage12가_있을때만_업데이트메뉴를_표시한다()
    {
        var allowed = Filter(CreateAccess(
            CurrentUserAccessPolicy.AdminRole,
            CurrentUserAccessPolicy.UpdateManagePermissionCode));
        var denied = Filter(CreateAccess(CurrentUserAccessPolicy.AdminRole));

        Assert.Contains(allowed, menu => menu.Key == "updates");
        Assert.DoesNotContain(denied, menu => menu.Key == "updates");
    }

    [Theory]
    [InlineData(CurrentUserAccessPolicy.PartnerUserRole)]
    [InlineData(3)]
    [InlineData(99)]
    public void Filter_PartnerUser와_기타역할은_업데이트메뉴를_숨긴다(int role)
    {
        var menus = Filter(CreateAccess(
            role,
            CurrentUserAccessPolicy.UpdateManagePermissionCode));

        Assert.DoesNotContain(menus, menu => menu.Key == "updates");
    }

    [Fact]
    public void Filter_접근정보가_없으면_업데이트메뉴를_숨긴다()
    {
        var menus = Filter(access: null);

        Assert.DoesNotContain(menus, menu => menu.Key == "updates");
    }

    [Fact]
    public void Filter_권한없는자식만_남은_부모그룹을_숨긴다()
    {
        var source = new List<MenuItem>
        {
            new()
            {
                Key = "restricted-parent",
                Title = "제한 그룹",
                Order = 1,
                Children = new List<MenuItem>
                {
                    new()
                    {
                        Key = "restricted-child",
                        Title = "제한 메뉴",
                        Url = "restricted",
                        Order = 1,
                        Roles = new List<int>
                        {
                            CurrentUserAccessPolicy.SystemRole,
                            CurrentUserAccessPolicy.AdminRole
                        },
                        RequiredPermissionCode =
                            CurrentUserAccessPolicy.UpdateManagePermissionCode
                    }
                }
            }
        };

        var result = _filter.Filter(
            source,
            CreateAccess(CurrentUserAccessPolicy.AdminRole));

        Assert.Empty(result);
    }

    [Fact]
    public void Filter_기존메뉴의_순서와구조를_유지하고_원본을_변경하지않는다()
    {
        var source = MenuConfiguration.GetMenus();
        var originalKeys = source
            .Where(menu => menu.Key != "updates")
            .OrderBy(menu => menu.Order)
            .Select(menu => menu.Key)
            .ToArray();

        var result = _filter.Filter(
            source,
            CreateAccess(CurrentUserAccessPolicy.AdminRole));

        var resultKeys = result
            .OrderBy(menu => menu.Order)
            .Select(menu => menu.Key)
            .ToArray();

        Assert.Equal(originalKeys, resultKeys);
        Assert.Contains(source, menu => menu.Key == "updates");
        Assert.DoesNotContain(result, menu => menu.Key == "updates");
    }

    private List<MenuItem> Filter(CurrentUserAccessResponse? access)
    {
        return _filter.Filter(MenuConfiguration.GetMenus(), access);
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
