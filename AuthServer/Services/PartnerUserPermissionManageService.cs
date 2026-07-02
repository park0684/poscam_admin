using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.UserManage;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Repositories;

namespace poscam.AuthServer.Services;

public class PartnerUserPermissionManageService
{
    private readonly UserAccountRepository _userAccountRepository;
    private readonly PartnerUserPermissionRepository _permissionRepository;
    private readonly AdminPermissionService _adminPermissionService;

    public PartnerUserPermissionManageService(
        UserAccountRepository userAccountRepository,
        PartnerUserPermissionRepository permissionRepository,
        AdminPermissionService adminPermissionService)
    {
        _userAccountRepository = userAccountRepository;
        _permissionRepository = permissionRepository;
        _adminPermissionService = adminPermissionService;
    }

    public async Task<ApiResponse<List<int>>> GetPermissionsAsync(
        int userCode,
        UserAccount loginUser)
    {
        var accessResult = await CheckManagePermissionAsync(loginUser);

        if (!accessResult.Success)
        {
            return ApiResponse<List<int>>.Fail(
                accessResult.ErrorCode,
                accessResult.Message);
        }

        var targetResult = await GetTargetPartnerUserAsync(userCode);

        if (!targetResult.Success || targetResult.Data == null)
        {
            return ApiResponse<List<int>>.Fail(
                targetResult.ErrorCode,
                targetResult.Message);
        }

        var permissionCodes = await _permissionRepository.GetPermissionCodesAsync(
            userCode);

        return ApiResponse<List<int>>.Ok(
            permissionCodes,
            "담당자 권한을 조회했습니다.");
    }

    public async Task<ApiResponse<bool>> UpdatePermissionsAsync(
        int userCode,
        PartnerUserPermissionUpdateRequest request,
        UserAccount loginUser)
    {
        var accessResult = await CheckManagePermissionAsync(loginUser);

        if (!accessResult.Success)
        {
            return ApiResponse<bool>.Fail(
                accessResult.ErrorCode,
                accessResult.Message);
        }

        if (request.UserCode != userCode)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.ValidationError,
                "권한 수정 대상이 올바르지 않습니다.");
        }

        var targetResult = await GetTargetPartnerUserAsync(userCode);

        if (!targetResult.Success || targetResult.Data == null)
        {
            return ApiResponse<bool>.Fail(
                targetResult.ErrorCode,
                targetResult.Message);
        }

        var permissionCodes = NormalizePermissionCodes(
            request.PermissionCodes);

        await _permissionRepository.ReplacePermissionsAsync(
            userCode,
            permissionCodes,
            loginUser.UserCode);

        return ApiResponse<bool>.Ok(
            true,
            "담당자 권한이 저장되었습니다.");
    }

    private Task<ApiResponse<bool>> CheckManagePermissionAsync(
        UserAccount loginUser)
    {
        return _adminPermissionService.CheckPermissionAsync(
            loginUser,
            AdminPermissionType.PartnerUserManage);
    }

    private async Task<ApiResponse<UserAccount>> GetTargetPartnerUserAsync(
        int userCode)
    {
        if (userCode <= 0)
        {
            return ApiResponse<UserAccount>.Fail(
                AuthErrorCode.InvalidLogin,
                "담당자 코드가 올바르지 않습니다.");
        }

        var targetUser = await _userAccountRepository.GetByCodeAsync(userCode);

        if (targetUser == null ||
            targetUser.UserRole != (int)UserRole.PartnerUser)
        {
            return ApiResponse<UserAccount>.Fail(
                AuthErrorCode.InvalidLogin,
                "담당자 계정을 찾을 수 없습니다.");
        }

        return ApiResponse<UserAccount>.Ok(targetUser);
    }

    private static List<int> NormalizePermissionCodes(
        List<int>? permissionCodes)
    {
        if (permissionCodes == null || permissionCodes.Count == 0)
        {
            return new List<int>();
        }

        var validCodes = Enum.GetValues<PartnerUserPermissionType>()
            .Select(x => (int)x)
            .ToHashSet();

        return permissionCodes
            .Where(validCodes.Contains)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
    }
}
