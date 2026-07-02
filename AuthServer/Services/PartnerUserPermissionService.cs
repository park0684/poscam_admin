using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Repositories;

namespace poscam.AuthServer.Services;

/// <summary>
/// 파트너 담당자의 세부 권한을 검증한다.
///
/// System과 Admin 권한은 이 서비스가 처리하지 않으며,
/// PartnerUser 계정만 partner_user_permissions 테이블을 기준으로 확인한다.
/// </summary>
public class PartnerUserPermissionService
{
    private readonly IPartnerUserPermissionReader _permissionReader;

    public PartnerUserPermissionService(
        IPartnerUserPermissionReader permissionReader)
    {
        _permissionReader = permissionReader;
    }

    public async Task<ApiResponse<bool>> CheckPermissionAsync(
        UserAccount loginUser,
        PartnerUserPermissionType permission)
    {
        if (loginUser == null)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        if ((UserRole)loginUser.UserRole != UserRole.PartnerUser)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.PermissionDenied,
                "파트너 담당자 기능을 사용할 권한이 없습니다.");
        }

        var hasPermission = await _permissionReader.ExistsPermissionAsync(
            loginUser.UserCode,
            permission);

        if (!hasPermission)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.PermissionDenied,
                "해당 기능을 실행할 권한이 없습니다.");
        }

        return ApiResponse<bool>.Ok(true);
    }
}
