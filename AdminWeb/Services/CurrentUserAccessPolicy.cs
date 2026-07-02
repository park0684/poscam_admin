using poscam.AdminWeb.Models.Account;

namespace poscam.AdminWeb.Services;

/// <summary>
/// AdminWeb에서 현재 사용자 접근정보를 해석하는 순수 정책.
/// 실제 보안 경계는 각 백엔드 API의 권한 검증이며,
/// 이 정책은 메뉴와 화면을 사전에 숨기기 위한 보조 UI 경계이다.
/// </summary>
public static class CurrentUserAccessPolicy
{
    public const int SystemRole = 0;
    public const int AdminRole = 1;
    public const int PartnerUserRole = 2;

    public const int PartnerUserManagePermissionCode = 5;
    public const int StoreManagePermissionCode = 7;
    public const int UpdateManagePermissionCode = 12;

    public static bool CanManagePartnerUsers(
        CurrentUserAccessResponse? access)
    {
        return CanUsePermission(
            access,
            PartnerUserManagePermissionCode,
            AdminRole,
            PartnerUserRole);
    }

    public static bool CanManagePartnerUserPermissions(
        CurrentUserAccessResponse? access)
    {
        return CanUsePermission(
            access,
            PartnerUserManagePermissionCode,
            AdminRole);
    }

    public static bool CanManageStores(
        CurrentUserAccessResponse? access)
    {
        return CanUsePermission(
            access,
            StoreManagePermissionCode,
            AdminRole,
            PartnerUserRole);
    }

    public static bool CanManageUpdates(CurrentUserAccessResponse? access)
    {
        return CanUsePermission(
            access,
            UpdateManagePermissionCode,
            AdminRole);
    }

    private static bool CanUsePermission(
        CurrentUserAccessResponse? access,
        int permissionCode,
        params int[] allowedRoles)
    {
        if (access is null)
        {
            return false;
        }

        if (access.UserRole == SystemRole)
        {
            return true;
        }

        return allowedRoles.Contains(access.UserRole)
               && access.PermissionCodes.Contains(permissionCode);
    }
}
