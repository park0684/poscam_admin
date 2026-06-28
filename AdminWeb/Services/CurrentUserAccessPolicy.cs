using poscam.AdminWeb.Models.Account;

namespace poscam.AdminWeb.Services;

/// <summary>
/// AdminWeb에서 현재 사용자 접근정보를 해석하는 순수 정책.
/// 실제 보안 경계는 UpdateServer의 매 요청 AuthServer 권한 확인이며,
/// 이 정책은 메뉴와 화면을 사전에 숨기기 위한 보조 UI 경계이다.
/// </summary>
public static class CurrentUserAccessPolicy
{
    public const int SystemRole = 0;
    public const int AdminRole = 1;
    public const int PartnerUserRole = 2;
    public const int UpdateManagePermissionCode = 12;

    public static bool CanManageUpdates(CurrentUserAccessResponse? access)
    {
        if (access is null)
        {
            return false;
        }

        if (access.UserRole == SystemRole)
        {
            return true;
        }

        return access.UserRole == AdminRole
               && access.PermissionCodes.Contains(UpdateManagePermissionCode);
    }
}
