using poscam.AuthServer.Models.Dtos.Admin;
using poscam.AuthServer.Models.Dtos.Account;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Repositories;
using poscam.AuthServer.Models.Dtos.UserManage;

namespace poscam.AuthServer.Services;

/// <summary>
/// 관리자 계정 관리 서비스.
/// 
/// 관리자 계정 생성, 수정, 비밀번호 초기화,
/// 관리자 세부 권한 조회/수정을 담당한다.
/// 
/// System 계정은 모든 관리자 계정 관리 기능을 사용할 수 있고,
/// Admin 계정은 부여된 세부 권한에 따라 기능이 제한된다.
/// </summary>
public class AdminAccountManageService
{
    private readonly UserAccountRepository _userAccountRepository;
    private readonly AdminUserPermissionRepository _adminUserPermissionRepository;
    private readonly AdminPermissionService _adminPermissionService;
    private readonly PasswordHashService _passwordHashService;

    public AdminAccountManageService(
        UserAccountRepository userAccountRepository,
        AdminUserPermissionRepository adminUserPermissionRepository,
        AdminPermissionService adminPermissionService,
        PasswordHashService passwordHashService)
    {
        _userAccountRepository = userAccountRepository;
        _adminUserPermissionRepository = adminUserPermissionRepository;
        _adminPermissionService = adminPermissionService;
        _passwordHashService = passwordHashService;
    }

    /// <summary>
    /// 관리자 계정 관리 권한을 확인한다.
    /// </summary>
    private Task<ApiResponse<bool>> CheckAdminAccountManagePermissionAsync(
        UserAccount loginUser)
    {
        return _adminPermissionService.CheckPermissionAsync(
            loginUser,
            AdminPermissionType.AdminAccountManage);
    }

    /// <summary>
    /// 관리자 비밀번호 초기화 권한을 확인한다.
    /// </summary>
    private Task<ApiResponse<bool>> CheckAdminPasswordResetPermissionAsync(
        UserAccount loginUser)
    {
        return _adminPermissionService.CheckPermissionAsync(
            loginUser,
            AdminPermissionType.AdminPasswordReset);
    }

    /// <summary>
    /// 관리자 권한 부여/수정 권한을 확인한다.
    /// </summary>
    private Task<ApiResponse<bool>> CheckAdminPermissionManagePermissionAsync(
        UserAccount loginUser)
    {
        return _adminPermissionService.CheckPermissionAsync(
            loginUser,
            AdminPermissionType.AdminPermissionManage);
    }

    /// <summary>
    /// 관리자 계정에 사용할 수 있는 상태인지 확인한다.
    /// 
    /// 관리자 계정은 가입 승인 흐름을 타지 않으므로 Pending은 사용하지 않는다.
    /// </summary>
    private static bool IsValidAdminStatus(int userStatus)
    {
        return userStatus is
            (int)UserStatus.Active or
            (int)UserStatus.Suspended or
            (int)UserStatus.Invalid or
            (int)UserStatus.Blocked;
    }


    /// <summary>
    /// 전달된 권한 코드 목록에서 중복과 잘못된 코드를 제거한다.
    /// 
    /// DB에는 숫자 코드만 저장하지만,
    /// 저장 가능한 코드는 AdminPermissionType에 정의된 값으로 제한한다.
    /// </summary>
    private static List<int> NormalizePermissionCodes(
        List<int> permissionCodes)
    {
        if (permissionCodes == null || permissionCodes.Count == 0)
        {
            return new List<int>();
        }

        var validCodes = Enum.GetValues<AdminPermissionType>()
            .Select(x => (int)x)
            .ToHashSet();

        return permissionCodes
            .Where(x => validCodes.Contains(x))
            .Distinct()
            .OrderBy(x => x)
            .ToList();
    }


    /// <summary>
    /// 관리자 계정 목록을 조회한다.
    /// 
    /// System은 항상 허용되고,
    /// Admin은 AdminAccountManage 권한을 보유해야 조회할 수 있다.
    /// </summary>
    public async Task<ApiResponse<List<UserManageListItemDto>>> GetAdminAccountsAsync(
        int? userStatus,
        UserAccount loginUser)
    {
        var permissionResult = await CheckAdminAccountManagePermissionAsync(
            loginUser);

        if (!permissionResult.Success)
        {
            return ApiResponse<List<UserManageListItemDto>>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        var admins = await _userAccountRepository.GetAdminAccountListAsync(
            userStatus);

        return ApiResponse<List<UserManageListItemDto>>.Ok(
            admins.Select(ToListItemDto).ToList(),
            "관리자 계정 목록을 조회했습니다.");
    }

    /// <summary>
    /// 관리자 계정 상세 정보를 조회한다.
    /// 
    /// System은 항상 허용되고,
    /// Admin은 AdminAccountManage 권한을 보유해야 조회할 수 있다.
    /// </summary>
    public async Task<ApiResponse<UserManageDetailDto>> GetAdminAccountDetailAsync(
        int userCode,
        UserAccount loginUser)
    {
        var permissionResult = await CheckAdminAccountManagePermissionAsync(
            loginUser);

        if (!permissionResult.Success)
        {
            return ApiResponse<UserManageDetailDto>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        if (userCode <= 0)
        {
            return ApiResponse<UserManageDetailDto>.Fail(
                AuthErrorCode.InvalidLogin,
                "관리자 코드가 올바르지 않습니다.");
        }

        var admin = await _userAccountRepository.GetAdminAccountDetailAsync(
            userCode);

        if (admin == null)
        {
            return ApiResponse<UserManageDetailDto>.Fail(
                AuthErrorCode.InvalidLogin,
                "관리자 계정 정보를 찾을 수 없습니다.");
        }

        return ApiResponse<UserManageDetailDto>.Ok(
            ToDetailDto(admin),
            "관리자 계정 상세 정보를 조회했습니다.");
    }

    /// <summary>
    /// 관리자 계정을 신규 생성한다.
    /// 
    /// 권한 정책:
    /// - System은 생성 가능
    /// - Admin은 AdminAccountManage 권한이 있어야 생성 가능
    /// 
    /// 처리 순서:
    /// 1. 관리자 계정 생성 권한 확인
    /// 2. 입력값 검증
    /// 3. 로그인 ID 중복 확인
    /// 4. 비밀번호 해시 생성
    /// 5. users 테이블에 UserRole.Admin으로 저장
    /// 6. 권한 코드가 전달된 경우 admin_user_permissions에 저장
    /// </summary>
    public async Task<ApiResponse<UserSaveResponse>> CreateAdminAccountAsync(
        AdminAccountCreateRequest request,
        UserAccount loginUser)
    {
        var permissionResult = await CheckAdminAccountManagePermissionAsync(
            loginUser);

        if (!permissionResult.Success)
        {
            return ApiResponse<UserSaveResponse>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return ApiResponse<UserSaveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "관리자 로그인 ID를 입력하세요.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return ApiResponse<UserSaveResponse>.Fail(
                AuthErrorCode.InvalidPassword,
                "관리자 초기 비밀번호를 입력하세요.");
        }

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            return ApiResponse<UserSaveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "관리자 이름을 입력하세요.");
        }

        if (!IsValidAdminStatus(request.UserStatus))
        {
            return ApiResponse<UserSaveResponse>.Fail(
                AuthErrorCode.ValidationError,
                "관리자 계정 상태가 올바르지 않습니다.");
        }

        var userId = request.UserId.Trim();

        var exists = await _userAccountRepository.ExistsUserIdAsync(userId);

        if (exists)
        {
            return ApiResponse<UserSaveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "이미 사용 중인 로그인 ID입니다.");
        }

        var passwordHash = _passwordHashService.HashPassword(
            request.Password);

        var adminUser = new UserAccount
        {
            PartnerCode = null,
            UserId = userId,
            UserPasswordHash = passwordHash,
            UserName = request.UserName.Trim(),
            UserCell = request.UserCell?.Trim(),
            UserEmail = request.UserEmail?.Trim(),
            UserRole = (int)UserRole.Admin,
            UserStatus = request.UserStatus,
            ApprovedBy = loginUser.UserCode,
            ApprovedAt = DateTime.Now
        };

        var createdUserCode = await _userAccountRepository.InsertAdminAccountAsync(
            adminUser);

        if (request.PermissionCodes != null &&
            request.PermissionCodes.Count > 0)
        {
            var validPermissionCodes = NormalizePermissionCodes(
                request.PermissionCodes);

            await _adminUserPermissionRepository.ReplacePermissionsAsync(
                createdUserCode,
                validPermissionCodes,
                loginUser.UserCode);
        }

        return ApiResponse<UserSaveResponse>.Ok(
            new UserSaveResponse
            {
                UserCode = createdUserCode,
                PartnerCode = null,
                UserId = userId,
                UserName = adminUser.UserName,
                Created = true,
                Saved = true
            },
            "관리자 계정이 생성되었습니다.");
    }

    /// <summary>
    /// 관리자 계정 기본정보를 수정한다.
    /// 
    /// 권한 정책:
    /// - System은 수정 가능
    /// - Admin은 AdminAccountManage 권한이 있어야 수정 가능
    /// 
    /// 수정 대상:
    /// - 이름
    /// - 연락처
    /// - 이메일
    /// - 계정 상태
    /// 
    /// 비밀번호와 세부 권한은 별도 API에서 처리한다.
    /// </summary>
    public async Task<ApiResponse<UserSaveResponse>> UpdateAdminAccountAsync(
        int userCode,
        AdminAccountUpdateRequest request,
        UserAccount loginUser)
    {
        var permissionResult = await CheckAdminAccountManagePermissionAsync(
            loginUser);

        if (!permissionResult.Success)
        {
            return ApiResponse<UserSaveResponse>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        if (userCode <= 0 || request.UserCode != userCode)
        {
            return ApiResponse<UserSaveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "수정 대상 관리자 코드가 올바르지 않습니다.");
        }

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            return ApiResponse<UserSaveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "관리자 이름을 입력하세요.");
        }

        if (!IsValidAdminStatus(request.UserStatus))
        {
            return ApiResponse<UserSaveResponse>.Fail(
                AuthErrorCode.ValidationError,
                "관리자 계정 상태가 올바르지 않습니다.");
        }

        var existingAdmin = await _userAccountRepository.GetAdminAccountDetailAsync(
            userCode);

        if (existingAdmin == null)
        {
            return ApiResponse<UserSaveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "관리자 계정 정보를 찾을 수 없습니다.");
        }

        var updateUser = new UserAccount
        {
            UserCode = userCode,
            UserName = request.UserName.Trim(),
            UserCell = request.UserCell?.Trim(),
            UserEmail = request.UserEmail?.Trim(),
            UserStatus = request.UserStatus
        };

        var affected = await _userAccountRepository.UpdateAdminAccountInfoAsync(
            updateUser);

        if (affected <= 0)
        {
            return ApiResponse<UserSaveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "관리자 계정 정보가 수정되지 않았습니다.");
        }

        return ApiResponse<UserSaveResponse>.Ok(
            new UserSaveResponse
            {
                UserCode = userCode,
                PartnerCode = null,
                UserId = existingAdmin.UserId,
                UserName = updateUser.UserName,
                Created = false,
                Saved = true
            },
            "관리자 계정 정보가 수정되었습니다.");
    }

    private static UserManageListItemDto ToListItemDto(UserListItemDto user)
    {
        return new UserManageListItemDto
        {
            UserCode = user.UserCode,
            PartnerCode = user.PartnerCode,
            PartnerName = user.PartnerName,
            UserId = user.UserId,
            UserName = user.UserName,
            UserCell = user.UserCell,
            UserEmail = user.UserEmail,
            UserRole = user.UserRole,
            UserStatus = user.UserStatus,
            ApprovedBy = user.ApprovedBy,
            ApprovedAt = user.ApprovedAt,
            UserRdate = user.UserRdate,
            UserUdate = user.UserUdate,
            UserRequestType = user.UserRequestType,
            UserRequestStatus = user.UserRequestStatus,
            UserRequestReason = user.UserRequestReason,
            UserRequestedBy = user.UserRequestedBy,
            UserRequestedAt = user.UserRequestedAt,
            UserRequestResultMemo = user.UserRequestResultMemo
        };
    }

    private static UserManageDetailDto ToDetailDto(UserAccount user)
    {
        return new UserManageDetailDto
        {
            UserCode = user.UserCode,
            PartnerCode = user.PartnerCode,
            UserId = user.UserId,
            UserName = user.UserName,
            UserCell = user.UserCell,
            UserEmail = user.UserEmail,
            UserRole = user.UserRole,
            UserStatus = user.UserStatus,
            ApprovedBy = user.ApprovedBy,
            ApprovedAt = user.ApprovedAt,
            UserRdate = user.UserRDate,
            UserUdate = user.UserUDate,
            UserRequestType = user.UserRequestType,
            UserRequestStatus = user.UserRequestStatus,
            UserRequestReason = user.UserRequestReason,
            UserRequestedBy = user.UserRequestedBy,
            UserRequestedAt = user.UserRequestedAt,
            UserRequestResultMemo = user.UserRequestResultMemo,

            // 관리자 계정 관리 화면에서는 상세 조회 권한을 통과한 계정만 접근하므로 true 처리.
            CanEdit = true,
            CanRequestChange = false,
            CanChangeStatus = true
        };
    }

    /// <summary>
    /// 관리자 계정 비밀번호를 초기화한다.
    /// 
    /// 권한 정책:
    /// - System은 초기화 가능
    /// - Admin은 AdminPasswordReset 권한이 있어야 초기화 가능
    /// 
    /// 주의:
    /// - System 계정은 초기화 대상에서 제외한다.
    /// - 대상은 UserRole.Admin 계정만 허용한다.
    /// - 실제 저장 컬럼은 users.user_password_hash 이다.
    /// </summary>
    public async Task<ApiResponse<bool>> ResetAdminPasswordAsync(
        int userCode,
        UserPasswordChangeRequest request,
        UserAccount loginUser)
    {
        var permissionResult = await CheckAdminPasswordResetPermissionAsync(
            loginUser);

        if (!permissionResult.Success)
        {
            return ApiResponse<bool>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        if (userCode <= 0)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "관리자 코드가 올바르지 않습니다.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidPassword,
                "새 비밀번호를 입력하세요.");
        }

        if (request.NewPassword.Length < 4)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidPassword,
                "새 비밀번호는 4자리 이상 입력하세요.");
        }

        var targetAdmin = await _userAccountRepository.GetAdminAccountDetailAsync(
            userCode);

        if (targetAdmin == null)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "관리자 계정 정보를 찾을 수 없습니다.");
        }

        var passwordHash = _passwordHashService.HashPassword(
            request.NewPassword);

        var affected = await _userAccountRepository.UpdatePasswordAsync(
            userCode,
            passwordHash);

        if (affected <= 0)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "관리자 비밀번호가 초기화되지 않았습니다.");
        }

        return ApiResponse<bool>.Ok(
            true,
            "관리자 비밀번호가 초기화되었습니다.");
    }

    /// <summary>
    /// 특정 관리자 계정에 부여된 세부 권한 목록을 조회한다.
    /// 
    /// 권한 정책:
    /// - System은 조회 가능
    /// - Admin은 AdminPermissionManage 권한이 있어야 조회 가능
    /// 
    /// 주의:
    /// - System 계정은 관리자 권한 테이블 관리 대상이 아니다.
    /// - 조회 대상은 UserRole.Admin 계정만 허용한다.
    /// </summary>
    public async Task<ApiResponse<List<int>>> GetAdminPermissionsAsync(
        int userCode,
        UserAccount loginUser)
    {
        var permissionResult = await CheckAdminPermissionManagePermissionAsync(
            loginUser);

        if (!permissionResult.Success)
        {
            return ApiResponse<List<int>>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        if (userCode <= 0)
        {
            return ApiResponse<List<int>>.Fail(
                AuthErrorCode.InvalidLogin,
                "관리자 코드가 올바르지 않습니다.");
        }

        var targetAdmin = await _userAccountRepository.GetAdminAccountDetailAsync(
            userCode);

        if (targetAdmin == null)
        {
            return ApiResponse<List<int>>.Fail(
                AuthErrorCode.InvalidLogin,
                "관리자 계정 정보를 찾을 수 없습니다.");
        }

        var permissionCodes = await _adminUserPermissionRepository
            .GetPermissionCodesAsync(userCode);

        return ApiResponse<List<int>>.Ok(
            permissionCodes,
            "관리자 권한 목록을 조회했습니다.");
    }

    /// <summary>
    /// 특정 관리자 계정의 세부 권한을 수정한다.
    /// 
    /// 권한 정책:
    /// - System은 수정 가능
    /// - Admin은 AdminPermissionManage 권한이 있어야 수정 가능
    /// 
    /// 처리 방식:
    /// - 기존 권한을 모두 삭제한다.
    /// - 요청으로 전달된 권한 코드 목록을 새로 저장한다.
    /// - DB에는 권한명 없이 숫자 코드만 저장한다.
    /// </summary>
    public async Task<ApiResponse<bool>> UpdateAdminPermissionsAsync(
        int userCode,
        AdminAccountPermissionUpdateRequest request,
        UserAccount loginUser)
    {
        var permissionResult = await CheckAdminPermissionManagePermissionAsync(
            loginUser);

        if (!permissionResult.Success)
        {
            return ApiResponse<bool>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        if (userCode <= 0 || request.UserCode != userCode)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "권한을 수정할 관리자 코드가 올바르지 않습니다.");
        }

        var targetAdmin = await _userAccountRepository.GetAdminAccountDetailAsync(
            userCode);

        if (targetAdmin == null)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "관리자 계정 정보를 찾을 수 없습니다.");
        }

        var normalizedPermissionCodes = NormalizePermissionCodes(
            request.PermissionCodes);

        await _adminUserPermissionRepository.ReplacePermissionsAsync(
            userCode,
            normalizedPermissionCodes,
            loginUser.UserCode);

        return ApiResponse<bool>.Ok(
            true,
            "관리자 권한이 수정되었습니다.");
    }


}