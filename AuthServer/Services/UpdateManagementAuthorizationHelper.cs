using poscam.AuthServer.Models.Dtos.Account;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;

namespace poscam.AuthServer.Services;

/// <summary>
/// UpdateServer 전용 권한 정책을 고정된 UpdateManage 권한으로 평가한다.
/// </summary>
public static class UpdateManagementAuthorizationHelper
{
    public static async Task<ApiResponse<UpdateManagementActorResponse>> AuthorizeActorAsync(
        UserAccount loginUser,
        Func<UserAccount, AdminPermissionType, Task<ApiResponse<bool>>> checkPermissionAsync)
    {
        var userRole = (UserRole)loginUser.UserRole;

        if (userRole == UserRole.PartnerUser)
        {
            return ApiResponse<UpdateManagementActorResponse>.Fail(
                AuthErrorCode.PermissionDenied,
                "업데이트 관리 권한이 없습니다.");
        }

        if (userRole == UserRole.Admin)
        {
            var permissionResult = await checkPermissionAsync(
                loginUser,
                AdminPermissionType.UpdateManage);

            if (!permissionResult.Success)
            {
                return ApiResponse<UpdateManagementActorResponse>.Fail(
                    permissionResult.ErrorCode,
                    permissionResult.Message);
            }
        }
        else if (userRole != UserRole.System)
        {
            return ApiResponse<UpdateManagementActorResponse>.Fail(
                AuthErrorCode.PermissionDenied,
                "업데이트 관리 권한이 없습니다.");
        }

        return ApiResponse<UpdateManagementActorResponse>.Ok(
            new UpdateManagementActorResponse
            {
                UserCode = loginUser.UserCode,
                UserName = loginUser.UserName,
                UserRole = loginUser.UserRole
            },
            "업데이트 관리 권한이 확인되었습니다.");
    }
}
