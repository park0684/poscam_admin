using Microsoft.AspNetCore.Mvc;
using poscam.AuthServer.Models.Dtos.Account;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Services;

namespace poscam.AuthServer.Controllers;

/// <summary>
/// 관리자/담당자 계정 API Controller.
/// 
/// 담당자 회원가입, 관리자/담당자 로그인,
/// 승인 대기 목록 조회, 담당자 승인/차단/일시중지를 담당한다.
/// </summary>
[ApiController]
public class AccountController : ControllerBase
{
    private readonly AccountService _accountService;

    public AccountController(AccountService accountService)
    {
        _accountService = accountService;
    }

    /// <summary>
    /// 담당자 회원가입 API.
    /// 
    /// 담당자는 가입 후 관리자 승인을 받아야 로그인할 수 있다.
    /// </summary>
    [HttpPost("api/accounts/register")]
    [ProducesResponseType(typeof(ApiResponse<UserRegisterResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserRegisterResponse>>> Register(
        [FromBody] UserRegisterRequest request)
    {
        var result = await _accountService.RegisterAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// 관리자/담당자 로그인 API.
    /// 
    /// 정상 승인된 계정이면 관리자 웹 API 호출용 토큰을 발급한다.
    /// </summary>
    [HttpPost("api/accounts/login")]
    [ProducesResponseType(typeof(ApiResponse<UserLoginResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserLoginResponse>>> Login(
        [FromBody] UserLoginRequest request)
    {
        var result = await _accountService.LoginAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// 승인 대기 사용자 목록 조회 API.
    /// 
    /// 관리자만 호출할 수 있다.
    /// Authorization: Bearer {accountToken}
    /// </summary>
    /// 
    [Obsolete("신규 담당자 관리는 UserManageController/UserManageService 기준 API를 사용하세요.")]
    [HttpGet("api/admin/users/pending")]
    [ProducesResponseType(typeof(ApiResponse<List<UserPendingListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<UserPendingListItemDto>>>> GetPendingUsers()
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<List<UserPendingListItemDto>>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        if (loginUserResult.Data.UserRole != (int)UserRole.Admin)
        {
            return Ok(ApiResponse<List<UserPendingListItemDto>>.Fail(
                AuthErrorCode.InvalidLogin,
                "관리자 권한이 필요합니다."));
        }

        var result = await _accountService.GetPendingUsersAsync();
        return Ok(result);
    }

    /// <summary>
    /// 담당자 승인 API.
    /// 
    /// 관리자만 호출할 수 있다.
    /// ApprovedBy 값은 요청 Body가 아니라 로그인 토큰의 관리자 user_code를 사용한다.
    /// </summary>
    [Obsolete("신규 담당자 관리는 UserManageController/UserManageService 기준 API를 사용하세요.")]
    [HttpPost("api/admin/users/approve")]
    [ProducesResponseType(typeof(ApiResponse<UserApproveResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserApproveResponse>>> Approve(
        [FromBody] UserApproveRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<UserApproveResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        if (loginUserResult.Data.UserRole != (int)UserRole.Admin)
        {
            return Ok(ApiResponse<UserApproveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "관리자 권한이 필요합니다."));
        }

        request.ApprovedBy = loginUserResult.Data.UserCode;

        var result = await _accountService.ApproveAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// 담당자 차단 API.
    /// 
    /// 관리자만 호출할 수 있다.
    /// 차단된 사용자는 로그인할 수 없다.
    /// </summary>
    [Obsolete("신규 담당자 관리는 UserManageController/UserManageService 기준 API를 사용하세요.")]
    [HttpPost("api/admin/users/{userCode:int}/block")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> Block(int userCode)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<bool>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        if (loginUserResult.Data.UserRole != (int)UserRole.Admin)
        {
            return Ok(ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "관리자 권한이 필요합니다."));
        }

        var result = await _accountService.BlockAsync(userCode);
        return Ok(result);
    }

    /// <summary>
    /// 담당자 일시중지 API.
    /// 
    /// 관리자만 호출할 수 있다.
    /// </summary>
    [Obsolete("신규 담당자 관리는 UserManageController/UserManageService 기준 API를 사용하세요.")]
    [HttpPost("api/admin/users/{userCode:int}/suspend")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> Suspend(int userCode)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<bool>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        if (loginUserResult.Data.UserRole != (int)UserRole.Admin)
        {
            return Ok(ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "관리자 권한이 필요합니다."));
        }

        var result = await _accountService.SuspendAsync(userCode);
        return Ok(result);
    }

    /// <summary>
    /// Authorization 헤더의 Bearer 토큰으로 로그인 사용자를 확인한다.
    /// 
    /// AccountService 내부에서 토큰 검증 후 users 테이블을 다시 조회하여
    /// 현재도 Active 상태인지 확인한다.
    /// </summary>
    private async Task<ApiResponse<UserAccount>> GetLoginUserAsync()
    {
        var authorizationHeader = Request.Headers.Authorization.FirstOrDefault();

        return await _accountService.GetLoginUserByTokenAsync(authorizationHeader);
    }

    /// <summary>
    /// 활성 담당자 목록 조회 API.
    /// 
    /// 매장 담당자 배정 화면에서 사용한다.
    /// 관리자만 호출할 수 있다.
    /// </summary>
    [HttpGet("api/admin/users/active")]
    [ProducesResponseType(typeof(ApiResponse<List<UserListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<UserListItemDto>>>> GetActiveUsers()
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<List<UserListItemDto>>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        if (loginUserResult.Data.UserRole != (int)UserRole.System &&
    loginUserResult.Data.UserRole != (int)UserRole.Admin)
        {
            return Ok(ApiResponse<List<UserListItemDto>>.Fail(
                AuthErrorCode.PermissionDenied,
                "담당자 목록을 조회할 권한이 없습니다."));
        }

        var result = await _accountService.GetActivePartnerUsersAsync();
        return Ok(result);
    }

    /// <summary>
    /// 특정 파트너사 내 활성 담당자 목록 조회 API.
    /// 
    /// 관리자:
    /// - 모든 파트너사 담당자 조회 가능
    /// 
    /// 담당자:
    /// - 본인 소속 파트너사의 담당자만 조회 가능
    /// </summary>
    [HttpGet("api/manage/partners/{partnerCode:int}/users/active")]
    [ProducesResponseType(typeof(ApiResponse<List<UserListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<UserListItemDto>>>> GetActiveUsersByPartner(
        int partnerCode)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<List<UserListItemDto>>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _accountService.GetActivePartnerUsersByPartnerAsync(
            partnerCode,
            loginUserResult.Data);

        return Ok(result);
    }
}