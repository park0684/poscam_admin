using Microsoft.AspNetCore.Mvc;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.UserManage;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Repositories;
using poscam.AuthServer.Services;

namespace poscam.AuthServer.Controllers;

/// <summary>
/// 관리자 웹의 담당자 계정 관리 API.
/// </summary>
[ApiController]
[Route("api/manage/users")]
public class UserManageController : ControllerBase
{
    private readonly UserManageService _userManageService;
    private readonly AccountService _accountService;
    private readonly PartnerUserPermissionService _partnerUserPermissionService;
    private readonly UserAccountRepository _userAccountRepository;
    private readonly UserLogRepository _userLogRepository;
    private readonly PasswordHashService _passwordHashService;

    public UserManageController(
        UserManageService userManageService,
        AccountService accountService,
        PartnerUserPermissionService partnerUserPermissionService,
        UserAccountRepository userAccountRepository,
        UserLogRepository userLogRepository,
        PasswordHashService passwordHashService)
    {
        _userManageService = userManageService;
        _accountService = accountService;
        _partnerUserPermissionService = partnerUserPermissionService;
        _userAccountRepository = userAccountRepository;
        _userLogRepository = userLogRepository;
        _passwordHashService = passwordHashService;
    }

    /// <summary>
    /// 담당자 목록 조회.
    /// PartnerUser는 PartnerUserManage(5) 권한과 본인 파트너사 범위를 모두 만족해야 한다.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<UserManageListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<UserManageListItemDto>>>> GetUsers(
        [FromQuery] int? partnerCode,
        [FromQuery] int? userStatus,
        [FromQuery] int? requestStatus)
    {
        var loginUserResult = await GetPartnerUserManageLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<List<UserManageListItemDto>>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _userManageService.GetUsersAsync(
            partnerCode,
            userStatus,
            requestStatus,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 담당자 상세 조회.
    /// </summary>
    [HttpGet("{userCode:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserManageDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserManageDetailDto>>> GetDetail(
        int userCode)
    {
        var loginUserResult = await GetPartnerUserManageLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<UserManageDetailDto>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _userManageService.GetDetailAsync(
            userCode,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 담당자 신규 등록.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserSaveResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserSaveResponse>>> CreateUser(
        [FromBody] UserCreateRequest request)
    {
        var loginUserResult = await GetPartnerUserManageLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<UserSaveResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _userManageService.CreateUserAsync(
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 담당자 정보 수정.
    /// System/Admin은 서비스의 관리자 권한 검사를 거치고,
    /// PartnerUser는 기존 정책대로 본인 연락처와 이메일만 수정할 수 있다.
    /// </summary>
    [HttpPut("{userCode:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserSaveResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserSaveResponse>>> UpdateUser(
        int userCode,
        [FromBody] UserUpdateRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<UserSaveResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        request.UserCode = userCode;

        var result = await _userManageService.UpdateUserAsync(
            userCode,
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 담당자 변경 요청 등록.
    /// </summary>
    [HttpPost("{userCode:int}/requests")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> CreateUserRequest(
        int userCode,
        [FromBody] UserRequestCreateRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<bool>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        request.UserCode = userCode;

        var result = await _userManageService.CreateUserRequestAsync(
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    [HttpPost("{userCode:int}/approve")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> ApproveUser(
        int userCode,
        [FromBody] UserRequestProcessRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<bool>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _userManageService.ApproveUserAsync(
            userCode,
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    [HttpPost("{userCode:int}/reject")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> RejectLatestRequest(
        int userCode,
        [FromBody] UserRequestProcessRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<bool>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _userManageService.RejectLatestRequestAsync(
            userCode,
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    [HttpPost("{userCode:int}/status")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> ChangeStatus(
        int userCode,
        [FromBody] UserStatusChangeRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<bool>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _userManageService.ChangeUserStatusAsync(
            userCode,
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 담당자 비밀번호 초기화.
    /// System/Admin은 기존 관리자 권한 정책을 사용하고,
    /// PartnerUser는 권한 9와 동일 파트너사 범위를 확인한다.
    /// </summary>
    [HttpPost("{userCode:int}/password-reset")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> ResetPassword(
        int userCode,
        [FromBody] UserPasswordChangeRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<bool>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var loginUser = loginUserResult.Data;

        if ((UserRole)loginUser.UserRole == UserRole.PartnerUser)
        {
            var partnerResult = await ResetPasswordByPartnerUserAsync(
                userCode,
                request,
                loginUser);

            return Ok(partnerResult);
        }

        var result = await _userManageService.ResetPasswordAsync(
            userCode,
            request,
            loginUser);

        return Ok(result);
    }

    /// <summary>
    /// 담당자 로그 조회.
    /// </summary>
    [HttpGet("{userCode:int}/logs")]
    [ProducesResponseType(typeof(ApiResponse<List<UserLogItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<UserLogItemDto>>>> GetUserLogs(
        int userCode)
    {
        var loginUserResult = await GetPartnerUserManageLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<List<UserLogItemDto>>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _userManageService.GetUserLogsAsync(
            userCode,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 담당자 본인 비밀번호 변경.
    /// </summary>
    [HttpPost("{userCode:int}/password-change")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> ChangeMyPassword(
        int userCode,
        [FromBody] UserPasswordSelfChangeRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<bool>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _userManageService.ChangeMyPasswordAsync(
            userCode,
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 최근 담당자 요청 처리 완료.
    /// </summary>
    [HttpPost("{userCode:int}/requests/process")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> ProcessLatestRequest(
        int userCode,
        [FromBody] UserRequestProcessRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<bool>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _userManageService.ProcessLatestRequestAsync(
            userCode,
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// PartnerUserPasswordReset(9) 권한이 있는 PartnerUser가
    /// 같은 파트너사의 다른 PartnerUser 비밀번호를 초기화한다.
    /// </summary>
    private async Task<ApiResponse<bool>> ResetPasswordByPartnerUserAsync(
        int userCode,
        UserPasswordChangeRequest request,
        UserAccount loginUser)
    {
        var permissionResult = await _partnerUserPermissionService.CheckPermissionAsync(
            loginUser,
            PartnerUserPermissionType.PartnerUserPasswordReset);

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
                "담당자 코드가 올바르지 않습니다.");
        }

        if (userCode == loginUser.UserCode)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.PermissionDenied,
                "본인 비밀번호는 내 비밀번호 변경 기능을 사용하세요.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "새 비밀번호를 입력하세요.");
        }

        if (loginUser.PartnerCode == null || loginUser.PartnerCode <= 0)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "담당자 계정에 파트너사가 지정되어 있지 않습니다.");
        }

        var targetUser = await _userAccountRepository.GetManageUserDetailAsync(userCode);

        if (targetUser == null ||
            targetUser.UserRole != (int)UserRole.PartnerUser)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "담당자 정보를 찾을 수 없습니다.");
        }

        if (targetUser.PartnerCode != loginUser.PartnerCode.Value)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.PermissionDenied,
                "본인 파트너사의 담당자 비밀번호만 초기화할 수 있습니다.");
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

        var memo = string.IsNullOrWhiteSpace(request.Memo)
            ? "파트너 담당자에 의한 비밀번호 초기화"
            : request.Memo;

        await _userLogRepository.InsertAsync(new UserLog
        {
            UserCode = targetUser.UserCode,
            PartnerCode = targetUser.PartnerCode,
            UlogType = (int)UserLogType.PasswordReset,
            UlogRequestType = (int)UserRequestType.PasswordReset,
            UlogRequestStatus = (int)UserRequestStatus.Completed,
            UlogMemo = memo,
            UlogProcessedBy = loginUser.UserCode,
            UlogProcessedAt = DateTime.Now
        });

        if (targetUser.UserRequestType == (int)UserRequestType.PasswordReset &&
            targetUser.UserRequestStatus == (int)UserRequestStatus.Pending)
        {
            await _userAccountRepository.CompleteLatestRequestAsync(
                userCode,
                (int)UserRequestType.PasswordReset,
                memo);
        }

        return ApiResponse<bool>.Ok(
            true,
            "비밀번호가 초기화되었습니다.");
    }

    /// <summary>
    /// 담당자 조회·등록 기능의 역할별 접근을 확인한다.
    /// System/Admin의 세부 권한 검사는 기존 UserManageService에서 수행한다.
    /// PartnerUser는 PartnerUserManage(5) 권한을 추가로 확인한다.
    /// </summary>
    private async Task<ApiResponse<UserAccount>> GetPartnerUserManageLoginUserAsync()
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return loginUserResult;
        }

        var loginUser = loginUserResult.Data;
        var role = (UserRole)loginUser.UserRole;

        if (role == UserRole.System || role == UserRole.Admin)
        {
            return ApiResponse<UserAccount>.Ok(loginUser);
        }

        if (role != UserRole.PartnerUser)
        {
            return ApiResponse<UserAccount>.Fail(
                AuthErrorCode.PermissionDenied,
                "담당자 관리 기능을 사용할 권한이 없습니다.");
        }

        var permissionResult = await _partnerUserPermissionService.CheckPermissionAsync(
            loginUser,
            PartnerUserPermissionType.PartnerUserManage);

        if (!permissionResult.Success)
        {
            return ApiResponse<UserAccount>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        return ApiResponse<UserAccount>.Ok(loginUser);
    }

    private async Task<ApiResponse<UserAccount>> GetLoginUserAsync()
    {
        var authorizationHeader = Request.Headers.Authorization.FirstOrDefault();

        return await _accountService.GetLoginUserByTokenAsync(authorizationHeader);
    }
}
