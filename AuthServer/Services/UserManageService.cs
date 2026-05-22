using System.Text.Json;
using poscam.AuthServer.Models.Dtos.Account;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.UserManage;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Repositories;

namespace poscam.AuthServer.Services;

/// <summary>
/// 직원/담당자 계정 관리 Service.
///
/// users 테이블과 userlog 테이블을 이용하여
/// 파트너사 담당자 계정의 등록, 조회, 수정, 승인, 상태 변경, 요청 등록을 처리한다.
///
/// 중요 DB 기준:
/// - users.user_password_hash 컬럼은 UserAccount.UserPasswordHash 속성에 매핑한다.
/// - users.approved_by, users.approved_at은 승인 처리자와 승인일로 사용한다.
/// - users.user_request_* 컬럼은 최신 요청 상태만 저장한다.
/// - userlog 테이블은 모든 요청/처리 이력을 누적 저장한다.
///
/// 권한 정책:
/// - 관리자: 전체 조회, 등록, 수정, 승인, 상태 변경, 비밀번호 초기화 가능.
/// - 담당자: 본인 파트너사 담당자 조회/등록 가능. 상태 변경은 직접 처리하지 않고 요청만 가능.
/// </summary>
public class UserManageService
{
    private readonly UserAccountRepository _userAccountRepository;
    private readonly UserLogRepository _userLogRepository;
    private readonly PartnerRepository _partnerRepository;
    private readonly PasswordHashService _passwordHashService;
    private readonly AdminPermissionService _adminPermissionService;

    /// <summary>
    /// UserManageService 생성자.
    ///
    /// PasswordService의 실제 메서드명이 다르면 아래 CreateUserAsync, ResetPasswordAsync의
    /// HashPassword 호출 부분만 현재 프로젝트 메서드명에 맞게 변경하면 된다.
    /// </summary>
    public UserManageService(
        UserAccountRepository userAccountRepository,
        UserLogRepository userLogRepository,
        PartnerRepository partnerRepository,
        PasswordHashService passwordHashService,
        AdminPermissionService adminPermissionService)
    {
        _userAccountRepository = userAccountRepository;
        _userLogRepository = userLogRepository;
        _partnerRepository = partnerRepository;
        _passwordHashService = passwordHashService;
        _adminPermissionService = adminPermissionService;
    }

    /// <summary>
    /// 담당자 목록 조회.
    ///
    /// 관리자:
    /// - partnerCode가 null이면 전체 파트너사 담당자 조회.
    /// - partnerCode가 있으면 해당 파트너사 담당자만 조회.
    ///
    /// 담당자:
    /// - 본인 PartnerCode의 담당자만 조회 가능.
    /// - 다른 파트너사 담당자 조회 불가.
    /// </summary>
    public async Task<ApiResponse<List<UserManageListItemDto>>> GetUsersAsync(
    int? partnerCode,
    int? userStatus,
    int? requestStatus,
    UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return ApiResponse<List<UserManageListItemDto>>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        var loginUserRole = (UserRole)loginUser.UserRole;

        // System / Admin은 세부 권한 확인 후 전체 조회 가능
        if (loginUserRole == UserRole.System ||
            loginUserRole == UserRole.Admin)
        {
            var permissionResult = await CheckPartnerUserManagePermissionAsync(loginUser);

            if (!permissionResult.Success)
            {
                return ApiResponse<List<UserManageListItemDto>>.Fail(
                    permissionResult.ErrorCode,
                    permissionResult.Message);
            }

            var list = await _userAccountRepository.GetManageUserListAsync(
                partnerCode,
                userStatus,
                requestStatus);

            return ApiResponse<List<UserManageListItemDto>>.Ok(
                list.Select(ToListItemDto).ToList(),
                "담당자 목록을 조회했습니다.");
        }

        // 파트너 담당자는 자기 파트너사 담당자만 조회 가능
        if (loginUserRole == UserRole.PartnerUser)
        {
            if (loginUser.PartnerCode == null)
            {
                return ApiResponse<List<UserManageListItemDto>>.Fail(
                    AuthErrorCode.InvalidLogin,
                    "담당자 계정에 파트너사가 지정되어 있지 않습니다.");
            }

            if (partnerCode != null &&
                partnerCode.Value != loginUser.PartnerCode.Value)
            {
                return ApiResponse<List<UserManageListItemDto>>.Fail(
                    AuthErrorCode.PermissionDenied,
                    "본인 파트너사의 담당자만 조회할 수 있습니다.");
            }

            var list = await _userAccountRepository.GetUsersByPartnerAsync(
                loginUser.PartnerCode.Value,
                userStatus,
                requestStatus);

            return ApiResponse<List<UserManageListItemDto>>.Ok(
                list.Select(ToListItemDto).ToList(),
                "담당자 목록을 조회했습니다.");
        }

        return ApiResponse<List<UserManageListItemDto>>.Fail(
            AuthErrorCode.PermissionDenied,
            "담당자 목록을 조회할 권한이 없습니다.");
    }

    /// <summary>
    /// 담당자 상세 조회.
    ///
    /// 관리자:
    /// - 전체 담당자 상세 조회 가능.
    ///
    /// 담당자:
    /// - 본인 소속 파트너사 담당자만 상세 조회 가능.
    /// </summary>
    public async Task<ApiResponse<UserManageDetailDto>> GetDetailAsync(
    int userCode,
    UserAccount loginUser)
    {
        if (userCode <= 0)
        {
            return ApiResponse<UserManageDetailDto>.Fail(
                AuthErrorCode.InvalidLogin,
                "사용자 코드가 올바르지 않습니다.");
        }

        var targetUser = await _userAccountRepository.GetManageUserDetailAsync(userCode);

        if (targetUser == null)
        {
            return ApiResponse<UserManageDetailDto>.Fail(
                AuthErrorCode.InvalidLogin,
                "담당자 정보를 찾을 수 없습니다.");
        }

        if (!await CanReadUserAsync(targetUser, loginUser))
        {
            return ApiResponse<UserManageDetailDto>.Fail(
                AuthErrorCode.PermissionDenied,
                "해당 담당자 정보를 조회할 권한이 없습니다.");
        }

        return ApiResponse<UserManageDetailDto>.Ok(
            ToDetailDto(targetUser, loginUser),
            "담당자 상세 정보를 조회했습니다.");
    }

    /// <summary>
    /// 담당자 신규 등록.
    ///
    /// 관리자:
    /// - 요청한 PartnerCode 기준으로 담당자 등록 가능.
    ///
    /// 담당자:
    /// - 본인 PartnerCode 기준으로만 담당자 등록 가능.
    ///
    /// 신규 등록된 담당자는 항상 승인대기 상태로 저장된다.
    /// </summary>
    public async Task<ApiResponse<UserSaveResponse>> CreateUserAsync(
        UserCreateRequest request,
        UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return ApiResponse<UserSaveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return ApiResponse<UserSaveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "아이디를 입력하세요.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return ApiResponse<UserSaveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "비밀번호를 입력하세요.");
        }

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            return ApiResponse<UserSaveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "담당자명을 입력하세요.");
        }
        
        var loginUserRole = (UserRole)loginUser.UserRole;

        if (loginUserRole == UserRole.System ||
            loginUserRole == UserRole.Admin)
        {
            var permissionResult = await CheckPartnerUserManagePermissionAsync(loginUser);

            if (!permissionResult.Success)
            {
                return ApiResponse<UserSaveResponse>.Fail(
                    permissionResult.ErrorCode,
                    permissionResult.Message);
            }
        }
        else if (loginUserRole != UserRole.PartnerUser)
        {
            return ApiResponse<UserSaveResponse>.Fail(
                AuthErrorCode.PermissionDenied,
                "담당자를 등록할 권한이 없습니다.");
        }

        var partnerResolveResult = await ResolvePartnerCodeForCreateAsync(
            request.PartnerCode,
            loginUser);

        if (!partnerResolveResult.Success)
        {
            return ApiResponse<UserSaveResponse>.Fail(
                partnerResolveResult.ErrorCode,
                partnerResolveResult.Message);
        }

        var partnerCode = partnerResolveResult.PartnerCode;

        if (partnerCode == null)
        {
            return ApiResponse<UserSaveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "파트너사 코드가 필요합니다.");
        }

        var exists = await _userAccountRepository.ExistsUserIdAsync(request.UserId);

        if (exists)
        {
            return ApiResponse<UserSaveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "이미 사용 중인 아이디입니다.");
        }

        var reason = string.IsNullOrWhiteSpace(request.RequestReason)
            ? "신규 담당자 가입 승인 요청"
            : request.RequestReason;

        // 실제 저장 컬럼은 users.user_password_hash 이다.
        // PasswordService의 실제 메서드명이 다르면 이 부분만 맞춰 수정한다.
        var passwordHash = _passwordHashService.HashPassword(request.Password);

        var user = new UserAccount
        {
            PartnerCode = partnerCode.Value,
            UserId = request.UserId.Trim(),
            UserPasswordHash = passwordHash,
            UserName = request.UserName.Trim(),
            UserCell = request.UserCell,
            UserEmail = request.UserEmail,
            UserRole = (int)UserRole.PartnerUser,
            UserStatus = (int)UserStatus.Pending,
            UserRequestType = (int)UserRequestType.JoinApproval,
            UserRequestStatus = (int)UserRequestStatus.Pending,
            UserRequestReason = reason,
            UserRequestedBy = loginUser.UserCode,
            UserRequestedAt = DateTime.Now
        };

        var createdUserCode = await _userAccountRepository.InsertPartnerUserAsync(user);

        await _userLogRepository.InsertAsync(new UserLog
        {
            UserCode = createdUserCode,
            PartnerCode = partnerCode.Value,
            UlogType = (int)UserLogType.Register,
            UlogRequestType = (int)UserRequestType.JoinApproval,
            UlogRequestStatus = (int)UserRequestStatus.Pending,
            UlogAfterStatus = (int)UserStatus.Pending,
            UlogReason = reason,
            UlogRequestedBy = loginUser.UserCode,
            UlogRequestedAt = DateTime.Now
        });

        await _userLogRepository.InsertAsync(new UserLog
        {
            UserCode = createdUserCode,
            PartnerCode = partnerCode.Value,
            UlogType = (int)UserLogType.ApprovalRequest,
            UlogRequestType = (int)UserRequestType.JoinApproval,
            UlogRequestStatus = (int)UserRequestStatus.Pending,
            UlogAfterStatus = (int)UserStatus.Pending,
            UlogReason = reason,
            UlogRequestedBy = loginUser.UserCode,
            UlogRequestedAt = DateTime.Now
        });

        return ApiResponse<UserSaveResponse>.Ok(
            new UserSaveResponse
            {
                UserCode = createdUserCode,
                PartnerCode = partnerCode.Value,
                UserId = request.UserId,
                UserName = request.UserName,
                Created = true,
                Saved = true
            },
            "담당자가 등록되었습니다. 관리자 승인 후 사용할 수 있습니다.");
    }

    /// <summary>
    /// 담당자 정보 수정.
    /// 실제 정보 수정은 관리자만 수행한다.
    /// 담당자는 별도 요청 API를 통해 변경 요청만 등록한다.
    /// </summary>
    public async Task<ApiResponse<UserSaveResponse>> UpdateUserAsync(
        int userCode,
        UserUpdateRequest request,
        UserAccount loginUser)
    {
        var permissionResult = await CheckPartnerUserManagePermissionAsync(loginUser);

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
                "수정 대상 사용자 코드가 올바르지 않습니다.");
        }

        var existingUser = await _userAccountRepository.GetManageUserDetailAsync(userCode);

        if (existingUser == null)
        {
            return ApiResponse<UserSaveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "담당자 정보를 찾을 수 없습니다.");
        }

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            return ApiResponse<UserSaveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "담당자명을 입력하세요.");
        }

        var changedFieldsJson = BuildChangedFieldsJson(existingUser, request);

        var updateUser = new UserAccount
        {
            UserCode = userCode,
            PartnerCode = request.PartnerCode,
            UserName = request.UserName.Trim(),
            UserCell = request.UserCell,
            UserEmail = request.UserEmail
        };

        var affected = await _userAccountRepository.UpdateUserInfoAsync(updateUser);

        if (affected <= 0)
        {
            return ApiResponse<UserSaveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "담당자 정보가 수정되지 않았습니다.");
        }

        await _userLogRepository.InsertAsync(new UserLog
        {
            UserCode = userCode,
            PartnerCode = request.PartnerCode,
            UlogType = (int)UserLogType.InfoChangeCompleted,
            UlogRequestType = (int)UserRequestType.InfoChange,
            UlogRequestStatus = (int)UserRequestStatus.Completed,
            UlogChangedFields = changedFieldsJson,
            UlogMemo = "관리자에 의한 담당자 정보 수정",
            UlogProcessedBy = loginUser.UserCode,
            UlogProcessedAt = DateTime.Now
        });

        return ApiResponse<UserSaveResponse>.Ok(
            new UserSaveResponse
            {
                UserCode = userCode,
                PartnerCode = request.PartnerCode,
                UserId = existingUser.UserId,
                UserName = request.UserName,
                Created = false,
                Saved = true
            },
            "담당자 정보가 수정되었습니다.");
    }

    /// <summary>
    /// 담당자 상태 변경 또는 정보 변경 요청 등록.
    /// 담당자는 직접 상태를 변경할 수 없고 요청만 등록한다.
    /// </summary>
    public async Task<ApiResponse<bool>> CreateUserRequestAsync(
        UserRequestCreateRequest request,
        UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        if (request.UserCode <= 0)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "요청 대상 사용자 코드가 올바르지 않습니다.");
        }

        var targetUser = await _userAccountRepository.GetManageUserDetailAsync(request.UserCode);

        if (targetUser == null)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "요청 대상 담당자 정보를 찾을 수 없습니다.");
        }

        if (!CanRequestForUser(targetUser, loginUser))
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "해당 담당자에 대한 요청을 등록할 권한이 없습니다.");
        }

        if (!IsValidRequestType(request.RequestType))
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "요청 유형이 올바르지 않습니다.");
        }

        var reason = string.IsNullOrWhiteSpace(request.RequestReason)
            ? "담당자 상태 변경 요청"
            : request.RequestReason;

        var affected = await _userAccountRepository.UpdateLatestRequestAsync(
            request.UserCode,
            request.RequestType,
            reason,
            loginUser.UserCode);

        if (affected <= 0)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "요청 상태가 저장되지 않았습니다.");
        }

        await _userLogRepository.InsertAsync(new UserLog
        {
            UserCode = targetUser.UserCode,
            PartnerCode = targetUser.PartnerCode,
            UlogType = ToRequestLogType(request.RequestType),
            UlogRequestType = request.RequestType,
            UlogRequestStatus = (int)UserRequestStatus.Pending,
            UlogBeforeStatus = targetUser.UserStatus,
            UlogReason = reason,
            UlogChangedFields = request.RequestedChangeJson,
            UlogRequestedBy = loginUser.UserCode,
            UlogRequestedAt = DateTime.Now
        });

        return ApiResponse<bool>.Ok(true, "요청이 등록되었습니다.");
    }

    /// <summary>
    /// 담당자 가입 승인 처리.
    /// 승인 처리는 관리자만 가능하다.
    /// </summary>
    public async Task<ApiResponse<bool>> ApproveUserAsync(
        int userCode,
        UserRequestProcessRequest request,
        UserAccount loginUser)
    {
        var permissionResult = await CheckPartnerUserManagePermissionAsync(loginUser);

        if (!permissionResult.Success)
        {
            return ApiResponse<bool>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        var targetUser = await _userAccountRepository.GetManageUserDetailAsync(userCode);

        if (targetUser == null)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "담당자 정보를 찾을 수 없습니다.");
        }

        if (targetUser.UserStatus != (int)UserStatus.Pending)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "승인대기 상태의 담당자만 승인할 수 있습니다.");
        }

        var memo = string.IsNullOrWhiteSpace(request.Memo)
            ? "승인 처리 완료"
            : request.Memo;

        var affected = await _userAccountRepository.ApproveUserAsync(
            userCode,
            loginUser.UserCode,
            memo);

        if (affected <= 0)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "승인 처리가 완료되지 않았습니다.");
        }

        await _userLogRepository.InsertAsync(new UserLog
        {
            UserCode = targetUser.UserCode,
            PartnerCode = targetUser.PartnerCode,
            UlogType = (int)UserLogType.ApprovalCompleted,
            UlogRequestType = (int)UserRequestType.JoinApproval,
            UlogRequestStatus = (int)UserRequestStatus.Completed,
            UlogBeforeStatus = (int)UserStatus.Pending,
            UlogAfterStatus = (int)UserStatus.Active,
            UlogMemo = memo,
            UlogProcessedBy = loginUser.UserCode,
            UlogProcessedAt = DateTime.Now
        });

        return ApiResponse<bool>.Ok(true, "담당자가 승인되었습니다.");
    }

    /// <summary>
    /// 담당자 최근 요청 반려.
    /// 반려 처리는 관리자만 가능하다.
    /// </summary>
    public async Task<ApiResponse<bool>> RejectLatestRequestAsync(
        int userCode,
        UserRequestProcessRequest request,
        UserAccount loginUser)
    {
        var permissionResult = await CheckPartnerUserManagePermissionAsync(loginUser);

        if (!permissionResult.Success)
        {
            return ApiResponse<bool>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        var targetUser = await _userAccountRepository.GetManageUserDetailAsync(userCode);

        if (targetUser == null)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "담당자 정보를 찾을 수 없습니다.");
        }

        if (targetUser.UserRequestStatus != (int)UserRequestStatus.Pending)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "요청대기 상태의 요청만 반려할 수 있습니다.");
        }

        var memo = string.IsNullOrWhiteSpace(request.Memo)
            ? "요청 반려"
            : request.Memo;

        var affected = await _userAccountRepository.RejectLatestRequestAsync(
            userCode,
            memo);

        if (affected <= 0)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "요청 반려가 처리되지 않았습니다.");
        }

        await _userLogRepository.InsertAsync(new UserLog
        {
            UserCode = targetUser.UserCode,
            PartnerCode = targetUser.PartnerCode,
            UlogType = ToRequestLogType(targetUser.UserRequestType ?? 0),
            UlogRequestType = targetUser.UserRequestType,
            UlogRequestStatus = (int)UserRequestStatus.Rejected,
            UlogBeforeStatus = targetUser.UserStatus,
            UlogReason = targetUser.UserRequestReason,
            UlogMemo = memo,
            UlogRequestedBy = targetUser.UserRequestedBy,
            UlogRequestedAt = targetUser.UserRequestedAt,
            UlogProcessedBy = loginUser.UserCode,
            UlogProcessedAt = DateTime.Now
        });

        return ApiResponse<bool>.Ok(true, "요청이 반려되었습니다.");
    }

    /// <summary>
    /// 담당자 최근 요청을 처리완료 상태로 변경한다.
    /// 
    /// 이 메서드는 실제 처리 API가 이미 실행된 뒤 호출된다.
    /// 정보수정/비밀번호초기화 요청에만 사용한다.
    /// 
    /// 처리 대상:
    /// - 정보수정 요청: UpdateUserAsync 성공 후 호출
    /// - 비밀번호초기화 요청: ResetPasswordAsync 성공 후 호출
    /// 
    /// 제외 대상:
    /// - 가입승인 요청은 ApproveUserAsync에서 완료 처리한다.
    /// - 상태 변경 요청은 ChangeUserStatusAsync에서 이미 요청 상태를 Completed로 변경한다.
    /// </summary>
    public async Task<ApiResponse<bool>> ProcessLatestRequestAsync(
        int userCode,
        UserRequestProcessRequest request,
        UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        if (userCode <= 0)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "담당자 코드가 올바르지 않습니다.");
        }

        var targetUser = await _userAccountRepository.GetManageUserDetailAsync(userCode);

        if (targetUser == null)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "담당자 정보를 찾을 수 없습니다.");
        }

        if (targetUser.UserRequestStatus != (int)UserRequestStatus.Pending)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "요청대기 상태의 요청만 처리할 수 있습니다.");
        }

        var requestType = targetUser.UserRequestType ?? 0;

        if (requestType != (int)UserRequestType.InfoChange &&
            requestType != (int)UserRequestType.PasswordReset)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "해당 요청은 전용 처리 API에서 이미 완료 처리됩니다.");
        }

        // 요청 유형별로 필요한 권한을 확인한다.
        // 정보수정 요청은 담당자 관리 권한이 필요하고,
        // 비밀번호초기화 요청은 담당자 비밀번호 초기화 권한이 필요하다.
        var permissionResult = requestType switch
        {
            (int)UserRequestType.PasswordReset =>
                await CheckPartnerUserPasswordResetPermissionAsync(loginUser),

            _ =>
                await CheckPartnerUserManagePermissionAsync(loginUser)
        };

        if (!permissionResult.Success)
        {
            return ApiResponse<bool>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        var memo = string.IsNullOrWhiteSpace(request.Memo)
            ? "담당자 요청 처리 완료"
            : request.Memo;

        var affected = await _userAccountRepository.CompleteLatestRequestAsync(
            userCode,
            requestType,
            memo);

        if (affected <= 0)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "요청 처리가 완료되지 않았습니다.");
        }

        return ApiResponse<bool>.Ok(true, "요청이 처리되었습니다.");
    }

    /// <summary>
    /// 관리자에 의한 사용자 상태 변경.
    /// 상태 변경은 관리자만 가능하다.
    /// </summary>
    public async Task<ApiResponse<bool>> ChangeUserStatusAsync(
        int userCode,
        UserStatusChangeRequest request,
        UserAccount loginUser)
    {
        var permissionResult = await CheckPartnerUserManagePermissionAsync(loginUser);

        if (!permissionResult.Success)
        {
            return ApiResponse<bool>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        if (!IsValidUserStatusForChange(request.NewStatus))
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "변경할 사용자 상태가 올바르지 않습니다.");
        }

        var targetUser = await _userAccountRepository.GetManageUserDetailAsync(userCode);

        if (targetUser == null)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "담당자 정보를 찾을 수 없습니다.");
        }

        var requestType = ToRequestTypeByStatus(request.NewStatus);
        var logType = ToCompletedLogTypeByStatus(request.NewStatus);

        var memo = string.IsNullOrWhiteSpace(request.Memo)
            ? "사용자 상태 변경"
            : request.Memo;

        var affected = await _userAccountRepository.ChangeUserStatusAsync(
            userCode,
            request.NewStatus,
            requestType,
            memo);

        if (affected <= 0)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "사용자 상태가 변경되지 않았습니다.");
        }

        await _userLogRepository.InsertAsync(new UserLog
        {
            UserCode = targetUser.UserCode,
            PartnerCode = targetUser.PartnerCode,
            UlogType = logType,
            UlogRequestType = requestType,
            UlogRequestStatus = (int)UserRequestStatus.Completed,
            UlogBeforeStatus = targetUser.UserStatus,
            UlogAfterStatus = request.NewStatus,
            UlogMemo = memo,
            UlogProcessedBy = loginUser.UserCode,
            UlogProcessedAt = DateTime.Now
        });

        return ApiResponse<bool>.Ok(true, "사용자 상태가 변경되었습니다.");
    }

    /// <summary>
    /// 관리자에 의한 담당자 비밀번호 초기화.
    /// 실제 저장 컬럼은 users.user_password_hash 이다.
    /// </summary>
    public async Task<ApiResponse<bool>> ResetPasswordAsync(
        int userCode,
        UserPasswordChangeRequest request,
        UserAccount loginUser)
    {
        var permissionResult = await CheckPartnerUserPasswordResetPermissionAsync(loginUser);

        if (!permissionResult.Success)
        {
            return ApiResponse<bool>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "새 비밀번호를 입력하세요.");
        }

        var targetUser = await _userAccountRepository.GetManageUserDetailAsync(userCode);

        if (targetUser == null)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "담당자 정보를 찾을 수 없습니다.");
        }

        var passwordHash = _passwordHashService.HashPassword(request.NewPassword);

        var affected = await _userAccountRepository.UpdatePasswordAsync(
            userCode,
            passwordHash);

        if (affected <= 0)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "비밀번호가 변경되지 않았습니다.");
        }

        await _userLogRepository.InsertAsync(new UserLog
        {
            UserCode = targetUser.UserCode,
            PartnerCode = targetUser.PartnerCode,
            UlogType = (int)UserLogType.PasswordReset,
            UlogRequestType = (int)UserRequestType.PasswordReset,
            UlogRequestStatus = (int)UserRequestStatus.Completed,
            UlogMemo = string.IsNullOrWhiteSpace(request.Memo)
                ? "관리자에 의한 비밀번호 초기화"
                : request.Memo,
            UlogProcessedBy = loginUser.UserCode,
            UlogProcessedAt = DateTime.Now
        });

        return ApiResponse<bool>.Ok(true, "비밀번호가 초기화되었습니다.");
    }

    /// <summary>
    /// 사용자 로그 조회.
    /// 관리자 또는 같은 파트너사 담당자만 조회할 수 있다.
    /// </summary>
    public async Task<ApiResponse<List<UserLogItemDto>>> GetUserLogsAsync(
        int userCode,
        UserAccount loginUser)
    {
        var targetUser = await _userAccountRepository.GetManageUserDetailAsync(userCode);

        if (targetUser == null)
        {
            return ApiResponse<List<UserLogItemDto>>.Fail(
                AuthErrorCode.InvalidLogin,
                "담당자 정보를 찾을 수 없습니다.");
        }

        if (!await CanReadUserAsync(targetUser, loginUser))
        {
            return ApiResponse<List<UserLogItemDto>>.Fail(
                AuthErrorCode.PermissionDenied,
                "해당 담당자 로그를 조회할 권한이 없습니다.");
        }

        var logs = await _userLogRepository.GetByUserCodeAsync(userCode);

        return ApiResponse<List<UserLogItemDto>>.Ok(
            logs.Select(ToLogItemDto).ToList(),
            "사용자 로그를 조회했습니다.");
    }

    /// <summary>
    /// System 또는 관리자 계정인지 확인한다.
    /// 
    /// System 계정은 관리자보다 상위 권한으로,
    /// 기존 관리자 전용 기능을 모두 사용할 수 있다.
    /// </summary>
    private bool IsSystemOrAdmin(UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return false;
        }

        return loginUser.UserRole == (int)UserRole.System ||
               loginUser.UserRole == (int)UserRole.Admin;
    }

    private bool IsPartnerUser(UserAccount loginUser)
    {
        return loginUser.UserRole == (int)UserRole.PartnerUser;
    }


    /// <summary>
    /// CanReadUserAsync 사용으로 대체
    /// </summary>
    /// <param name="targetUser"></param>
    /// <param name="loginUser"></param>
    /// <returns></returns>
    //private bool CanReadUser(UserAccount targetUser, UserAccount loginUser)
    //{
    //    if (IsSystemOrAdmin(loginUser))
    //    {
    //        return true;
    //    }

    //    return IsPartnerUser(loginUser)
    //           && loginUser.PartnerCode != null
    //           && targetUser.PartnerCode == loginUser.PartnerCode.Value;
    //}

    private bool CanRequestForUser(UserAccount targetUser, UserAccount loginUser)
    {
        return IsPartnerUser(loginUser)
               && loginUser.PartnerCode != null
               && targetUser.PartnerCode == loginUser.PartnerCode.Value;
    }

    private async Task<PartnerResolveResult> ResolvePartnerCodeForCreateAsync(
    int? requestedPartnerCode,
    UserAccount loginUser)
    {
        int? resolvedPartnerCode;

        var loginUserRole = (UserRole)loginUser.UserRole;

        if (loginUserRole == UserRole.System ||
            loginUserRole == UserRole.Admin)
        {
            resolvedPartnerCode = requestedPartnerCode;
        }
        else if (loginUserRole == UserRole.PartnerUser)
        {
            resolvedPartnerCode = loginUser.PartnerCode;
        }
        else
        {
            return PartnerResolveResult.Fail(
                AuthErrorCode.PermissionDenied,
                "담당자를 등록할 권한이 없습니다.");
        }

        if (resolvedPartnerCode == null || resolvedPartnerCode <= 0)
        {
            return PartnerResolveResult.Fail(
                AuthErrorCode.InvalidLogin,
                "파트너사 코드가 필요합니다.");
        }

        var partner = await _partnerRepository.GetByCodeAsync(
            resolvedPartnerCode.Value);

        if (partner == null)
        {
            return PartnerResolveResult.Fail(
                AuthErrorCode.InvalidLogin,
                "파트너사 정보를 찾을 수 없습니다.");
        }

        if (partner.PartnerStatus != 1)
        {
            return PartnerResolveResult.Fail(
                AuthErrorCode.InvalidLogin,
                "정상 상태의 파트너사에만 담당자를 등록할 수 있습니다.");
        }

        return PartnerResolveResult.Ok(resolvedPartnerCode.Value);
    }

    /// <summary>
    /// 담당자 본인 비밀번호 변경.
    /// 
    /// 관리자 초기화와 다른 점:
    /// - 관리자 권한이 필요하지 않음
    /// - 단, 로그인한 사용자가 본인 계정만 변경 가능
    /// - 현재 비밀번호가 일치해야 변경 가능
    /// - 실제 저장 컬럼은 users.user_password_hash
    /// </summary>
    public async Task<ApiResponse<bool>> ChangeMyPasswordAsync(
        int userCode,
        UserPasswordSelfChangeRequest request,
        UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        // URL의 userCode와 로그인 사용자의 UserCode가 다르면 차단합니다.
        // 즉, 담당자가 다른 담당자의 비밀번호를 바꿀 수 없도록 합니다.
        if (userCode != loginUser.UserCode)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "본인 계정의 비밀번호만 변경할 수 있습니다.");
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "현재 비밀번호를 입력하세요.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "새 비밀번호를 입력하세요.");
        }

        // 필요하면 여기에서 비밀번호 길이 정책을 추가할 수 있습니다.
        if (request.NewPassword.Length < 4)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "새 비밀번호는 4자리 이상 입력하세요.");
        }

        var targetUser = await _userAccountRepository.GetManageUserDetailAsync(userCode);

        if (targetUser == null)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "담당자 정보를 찾을 수 없습니다.");
        }

        var currentPasswordHash = await _userAccountRepository.GetPasswordHashAsync(userCode);

        if (string.IsNullOrWhiteSpace(currentPasswordHash))
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "현재 비밀번호 정보를 확인할 수 없습니다.");
        }

        var passwordMatched = _passwordHashService.VerifyPassword(
            request.CurrentPassword,
            currentPasswordHash);

        if (!passwordMatched)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "현재 비밀번호가 일치하지 않습니다.");
        }

        var newPasswordHash = _passwordHashService.HashPassword(request.NewPassword);

        var affected = await _userAccountRepository.UpdatePasswordAsync(
            userCode,
            newPasswordHash);

        if (affected <= 0)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "비밀번호가 변경되지 않았습니다.");
        }

        await _userLogRepository.InsertAsync(new UserLog
        {
            UserCode = targetUser.UserCode,
            PartnerCode = targetUser.PartnerCode,

            // 기존 화면의 로그 매핑 기준:
            // 7 => 비밀번호변경
            UlogType = (int)UserLogType.PasswordChanged,

            // 요청 유형은 비밀번호초기화/비밀번호 관련 요청 유형을 사용합니다.
            // enum 명칭이 PasswordReset으로 되어 있다면 그대로 사용합니다.
            UlogRequestType = (int)UserRequestType.PasswordReset,

            UlogRequestStatus = (int)UserRequestStatus.Completed,
            UlogMemo = "본인 비밀번호 변경",
            UlogProcessedBy = loginUser.UserCode,
            UlogProcessedAt = DateTime.Now
        });

        return ApiResponse<bool>.Ok(
            true,
            "비밀번호가 변경되었습니다.");
    }

    /// <summary>
    /// 특정 담당자 정보를 조회할 수 있는지 확인한다.
    /// 
    /// System / Admin:
    /// - PartnerUserManage 권한이 있어야 조회 가능
    /// 
    /// PartnerUser:
    /// - 본인 소속 파트너사의 담당자만 조회 가능
    /// </summary>
    private async Task<bool> CanReadUserAsync(
        UserAccount targetUser,
        UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return false;
        }

        var loginUserRole = (UserRole)loginUser.UserRole;

        if (loginUserRole == UserRole.System ||
            loginUserRole == UserRole.Admin)
        {
            var permissionResult = await CheckPartnerUserManagePermissionAsync(loginUser);
            return permissionResult.Success;
        }

        return loginUserRole == UserRole.PartnerUser
               && loginUser.PartnerCode != null
               && targetUser.PartnerCode == loginUser.PartnerCode.Value;
    }

    private static bool IsValidRequestType(int requestType)
    {
        return requestType is
            (int)UserRequestType.InfoChange or
            (int)UserRequestType.PasswordReset or
            (int)UserRequestType.Suspend or
            (int)UserRequestType.Restore or
            (int)UserRequestType.Invalid or
            (int)UserRequestType.Block;
    }

    private static bool IsValidUserStatusForChange(int newStatus)
    {
        return newStatus is
            (int)UserStatus.Active or
            (int)UserStatus.Suspended or
            (int)UserStatus.Invalid or
            (int)UserStatus.Blocked;
    }

    private static int ToRequestLogType(int requestType)
    {
        return requestType switch
        {
            (int)UserRequestType.InfoChange => (int)UserLogType.InfoChangeRequest,
            (int)UserRequestType.PasswordReset => (int)UserLogType.PasswordReset,
            (int)UserRequestType.Suspend => (int)UserLogType.SuspendRequest,
            (int)UserRequestType.Restore => (int)UserLogType.RestoreRequest,
            (int)UserRequestType.Invalid => (int)UserLogType.InvalidRequest,
            (int)UserRequestType.Block => (int)UserLogType.BlockRequest,
            (int)UserRequestType.JoinApproval => (int)UserLogType.ApprovalRequest,
            _ => (int)UserLogType.InfoChangeRequest
        };
    }

    private static int ToRequestTypeByStatus(int status)
    {
        return status switch
        {
            (int)UserStatus.Active => (int)UserRequestType.Restore,
            (int)UserStatus.Suspended => (int)UserRequestType.Suspend,
            (int)UserStatus.Invalid => (int)UserRequestType.Invalid,
            (int)UserStatus.Blocked => (int)UserRequestType.Block,
            _ => (int)UserRequestType.InfoChange
        };
    }

    private static int ToCompletedLogTypeByStatus(int status)
    {
        return status switch
        {
            (int)UserStatus.Active => (int)UserLogType.RestoreCompleted,
            (int)UserStatus.Suspended => (int)UserLogType.SuspendCompleted,
            (int)UserStatus.Invalid => (int)UserLogType.InvalidCompleted,
            (int)UserStatus.Blocked => (int)UserLogType.BlockCompleted,
            _ => (int)UserLogType.InfoChangeCompleted
        };
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

    private static UserManageDetailDto ToDetailDto(
        UserAccount user,
        UserAccount loginUser)
    {
        var canManage = loginUser.UserRole == (int)UserRole.System || loginUser.UserRole == (int)UserRole.Admin;

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
            CanEdit = canManage,
            CanRequestChange = !canManage,
            CanChangeStatus = canManage
        };
    }

    private static UserLogItemDto ToLogItemDto(UserLog log)
    {
        return new UserLogItemDto
        {
            UlogCode = log.UlogCode,
            UserCode = log.UserCode,
            PartnerCode = log.PartnerCode,
            UlogType = log.UlogType,
            UlogRequestType = log.UlogRequestType,
            UlogRequestStatus = log.UlogRequestStatus,
            UlogBeforeStatus = log.UlogBeforeStatus,
            UlogAfterStatus = log.UlogAfterStatus,
            UlogReason = log.UlogReason,
            UlogMemo = log.UlogMemo,
            UlogChangedFields = log.UlogChangedFields,
            UlogRequestedBy = log.UlogRequestedBy,
            UlogProcessedBy = log.UlogProcessedBy,
            UlogRequestedAt = log.UlogRequestedAt,
            UlogProcessedAt = log.UlogProcessedAt,
            UlogRdate = log.UlogRdate
        };
    }

    private static string? BuildChangedFieldsJson(
        UserAccount before,
        UserUpdateRequest after)
    {
        var changes = new Dictionary<string, object>();

        if (before.PartnerCode != after.PartnerCode)
        {
            changes["partnerCode"] = new { before = before.PartnerCode, after = after.PartnerCode };
        }

        if (before.UserName != after.UserName)
        {
            changes["userName"] = new { before = before.UserName, after = after.UserName };
        }

        if (before.UserCell != after.UserCell)
        {
            changes["userCell"] = new { before = before.UserCell, after = after.UserCell };
        }

        if (before.UserEmail != after.UserEmail)
        {
            changes["userEmail"] = new { before = before.UserEmail, after = after.UserEmail };
        }

        if (changes.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(changes);
    }

    private class PartnerResolveResult
    {
        public bool Success { get; set; }

        public AuthErrorCode ErrorCode { get; set; }

        public string Message { get; set; } = "";

        public int? PartnerCode { get; set; }

        public static PartnerResolveResult Ok(int partnerCode)
        {
            return new PartnerResolveResult
            {
                Success = true,
                PartnerCode = partnerCode,
                ErrorCode = AuthErrorCode.None
            };
        }

        public static PartnerResolveResult Fail(
            AuthErrorCode errorCode,
            string message)
        {
            return new PartnerResolveResult
            {
                Success = false,
                ErrorCode = errorCode,
                Message = message
            };
        }
    }

    /// <summary>
    /// 담당자 관리 권한을 확인한다.
    /// 
    /// System은 자동 허용되고,
    /// Admin은 PartnerUserManage 권한을 보유해야 한다.
    /// </summary>
    private Task<ApiResponse<bool>> CheckPartnerUserManagePermissionAsync(
        UserAccount loginUser)
    {
        return _adminPermissionService.CheckPermissionAsync(
            loginUser,
            AdminPermissionType.PartnerUserManage);
    }

    /// <summary>
    /// 담당자 비밀번호 초기화 권한을 확인한다.
    /// 
    /// System은 자동 허용되고,
    /// Admin은 PartnerUserPasswordReset 권한을 보유해야 한다.
    /// </summary>
    private Task<ApiResponse<bool>> CheckPartnerUserPasswordResetPermissionAsync(
        UserAccount loginUser)
    {
        return _adminPermissionService.CheckPermissionAsync(
            loginUser,
            AdminPermissionType.PartnerUserPasswordReset);
    }
}
