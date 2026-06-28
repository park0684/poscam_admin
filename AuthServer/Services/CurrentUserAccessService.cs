using poscam.AuthServer.Models.Dtos.Account;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Repositories;

namespace poscam.AuthServer.Services;

/// <summary>
/// 현재 로그인 사용자의 역할과 관리자 세부 권한을 조회한다.
/// </summary>
public class CurrentUserAccessService
{
    private readonly IAdminUserPermissionReader _permissionReader;

    public CurrentUserAccessService(IAdminUserPermissionReader permissionReader)
    {
        _permissionReader = permissionReader;
    }

    public async Task<ApiResponse<CurrentUserAccessResponse>> GetCurrentAccessAsync(
        UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return ApiResponse<CurrentUserAccessResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        var userRole = (UserRole)loginUser.UserRole;
        var permissionCodes = new List<int>();

        if (userRole == UserRole.Admin)
        {
            try
            {
                permissionCodes = await _permissionReader.GetPermissionCodesAsync(
                    loginUser.UserCode);
            }
            catch
            {
                return ApiResponse<CurrentUserAccessResponse>.Fail(
                    AuthErrorCode.DatabaseError,
                    "현재 사용자 권한을 조회하는 중 데이터베이스 오류가 발생했습니다.");
            }
        }
        else if (userRole != UserRole.System && userRole != UserRole.PartnerUser)
        {
            return ApiResponse<CurrentUserAccessResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "사용자 권한이 올바르지 않습니다.");
        }

        return ApiResponse<CurrentUserAccessResponse>.Ok(
            new CurrentUserAccessResponse
            {
                UserCode = loginUser.UserCode,
                UserName = loginUser.UserName,
                UserRole = loginUser.UserRole,
                PermissionCodes = permissionCodes
            },
            "현재 사용자 접근정보를 조회했습니다.");
    }
}
