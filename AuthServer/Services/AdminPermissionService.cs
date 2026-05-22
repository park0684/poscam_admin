using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Repositories;

namespace poscam.AuthServer.Services;

/// <summary>
/// 관리자 세부 권한 검증 서비스.
/// 
/// 권한 판단 기준:
/// - System 계정은 모든 관리자 기능을 사용할 수 있다.
/// - Admin 계정은 admin_user_permissions 테이블에 등록된 권한만 사용할 수 있다.
/// - PartnerUser 계정은 관리자 기능을 사용할 수 없다.
/// </summary>
public class AdminPermissionService
{
    private readonly AdminUserPermissionRepository _adminUserPermissionRepository;

    public AdminPermissionService(
        AdminUserPermissionRepository adminUserPermissionRepository)
    {
        _adminUserPermissionRepository = adminUserPermissionRepository;
    }

    /// <summary>
    /// 특정 관리자 권한을 보유하고 있는지 확인한다.
    /// 
    /// 사용 예:
    /// - 파트너사 등록/수정: PartnerManage
    /// - 담당자 등록/수정: PartnerUserManage
    /// - 매장 등록/수정: StoreManage
    /// </summary>
    public async Task<ApiResponse<bool>> CheckPermissionAsync(
        UserAccount loginUser,
        AdminPermissionType permission)
    {
        if (loginUser == null)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        var userRole = (UserRole)loginUser.UserRole;

        // System 계정은 모든 관리자 권한을 자동으로 허용한다.
        if (userRole == UserRole.System)
        {
            return ApiResponse<bool>.Ok(true);
        }

        // Admin이 아닌 계정은 관리자 권한 기능을 사용할 수 없다.
        if (userRole != UserRole.Admin)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.PermissionDenied,
                "관리자 기능을 사용할 권한이 없습니다.");
        }

        // Admin 계정은 실제 권한 테이블에 해당 권한이 존재하는지 확인한다.
        var hasPermission = await _adminUserPermissionRepository.ExistsPermissionAsync(
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

    /// <summary>
    /// 여러 관리자 권한 중 하나라도 보유하고 있는지 확인한다.
    /// 
    /// 예:
    /// 관리자 목록 조회처럼
    /// - 관리자 계정 생성/수정
    /// - 관리자 비밀번호 초기화
    /// - 관리자 권한 부여/수정
    /// 중 하나라도 있으면 접근을 허용하는 경우에 사용한다.
    /// </summary>
    public async Task<ApiResponse<bool>> CheckAnyPermissionAsync(
        UserAccount loginUser,
        params AdminPermissionType[] permissions)
    {
        if (loginUser == null)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        var userRole = (UserRole)loginUser.UserRole;

        // System 계정은 모든 관리자 권한을 자동으로 허용한다.
        if (userRole == UserRole.System)
        {
            return ApiResponse<bool>.Ok(true);
        }

        // Admin이 아닌 계정은 관리자 권한 기능을 사용할 수 없다.
        if (userRole != UserRole.Admin)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.PermissionDenied,
                "관리자 기능을 사용할 권한이 없습니다.");
        }

        if (permissions == null || permissions.Length == 0)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.ValidationError,
                "확인할 관리자 권한이 지정되지 않았습니다.");
        }

        foreach (var permission in permissions)
        {
            var hasPermission = await _adminUserPermissionRepository.ExistsPermissionAsync(
                loginUser.UserCode,
                permission);

            if (hasPermission)
            {
                return ApiResponse<bool>.Ok(true);
            }
        }

        return ApiResponse<bool>.Fail(
            AuthErrorCode.PermissionDenied,
            "해당 기능을 실행할 권한이 없습니다.");
    }
}