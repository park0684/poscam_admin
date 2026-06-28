using poscam.AdminWeb.Models.Account;
using poscam.AdminWeb.Models.Navigation;

namespace poscam.AdminWeb.Services;

/// <summary>
/// 현재 접근정보를 기준으로 메뉴를 복사·필터링한다.
/// 원본 MenuConfiguration 객체는 변경하지 않는다.
/// </summary>
public sealed class MenuAccessFilter
{
    public List<MenuItem> Filter(
        IEnumerable<MenuItem> menus,
        CurrentUserAccessResponse? access)
    {
        ArgumentNullException.ThrowIfNull(menus);

        return menus
            .OrderBy(menu => menu.Order)
            .Select(menu => FilterItem(menu, access))
            .Where(menu => menu is not null)
            .Cast<MenuItem>()
            .ToList();
    }

    private static MenuItem? FilterItem(
        MenuItem source,
        CurrentUserAccessResponse? access)
    {
        if (!CanAccess(source, access))
        {
            return null;
        }

        var filteredChildren = source.Children
            .OrderBy(child => child.Order)
            .Select(child => FilterItem(child, access))
            .Where(child => child is not null)
            .Cast<MenuItem>()
            .ToList();

        // 원래 자식 메뉴가 있던 부모에서 모든 자식이 제거되면
        // 클릭할 대상이 없는 빈 그룹이므로 부모도 숨긴다.
        if (source.Children.Count > 0 && filteredChildren.Count == 0)
        {
            return null;
        }

        return new MenuItem
        {
            Key = source.Key,
            Title = source.Title,
            Url = source.Url,
            Order = source.Order,
            Roles = source.Roles.ToList(),
            RequiredPermissionCode = source.RequiredPermissionCode,
            Children = filteredChildren
        };
    }

    private static bool CanAccess(
        MenuItem menu,
        CurrentUserAccessResponse? access)
    {
        var requiresRole = menu.Roles.Count > 0;
        var requiresPermission = menu.RequiredPermissionCode.HasValue;

        if (!requiresRole && !requiresPermission)
        {
            return true;
        }

        // 접근정보를 아직 조회하지 못했거나 조회에 실패한 경우
        // 권한이 필요한 메뉴는 기본적으로 숨긴다.
        if (access is null)
        {
            return false;
        }

        if (requiresRole && !menu.Roles.Contains(access.UserRole))
        {
            return false;
        }

        if (!requiresPermission)
        {
            return true;
        }

        // System은 세부 권한 목록과 무관하게 모든 관리자 기능을 허용한다.
        if (access.UserRole == CurrentUserAccessPolicy.SystemRole)
        {
            return true;
        }

        return access.UserRole == CurrentUserAccessPolicy.AdminRole
               && access.PermissionCodes.Contains(
                   menu.RequiredPermissionCode!.Value);
    }
}
