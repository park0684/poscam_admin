using Microsoft.AspNetCore.Mvc;
using poscam.AuthServer.Models.Dtos.Account;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Repositories;
using poscam.AuthServer.Services;

namespace poscam.AuthServer.Controllers;

[ApiController]
public class AccountController : ControllerBase
{
    private readonly AccountService _accountService;
    private readonly CurrentUserAccessService _currentUserAccessService;

    public AccountController(
        AccountService accountService,
        AdminUserPermissionRepository adminUserPermissionRepository,
        PartnerUserPermissionRepository partnerUserPermissionRepository)
    {
        _accountService = accountService;
        _currentUserAccessService = new CurrentUserAccessService(
            adminUserPermissionRepository,
            partnerUserPermissionRepository);
    }

    [HttpPost("api/accounts/register")]
    [ProducesResponseType(typeof(ApiResponse<UserRegisterResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserRegisterResponse>>> Register(
        [FromBody] UserRegisterRequest request)
    {
        var result = await _accountService.RegisterAsync(request);
        return Ok(result);
    }

    [HttpPost("api/accounts/login")]
    [ProducesResponseType(typeof(ApiResponse<UserLoginResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserLoginResponse>>> Login(
        [FromBody] UserLoginRequest request)
    {
        var result = await _accountService.LoginAsync(request);
        return Ok(result);
    }

    [HttpGet("api/accounts/me/access")]
    [ProducesResponseType(typeof(ApiResponse<CurrentUserAccessResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CurrentUserAccessResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<CurrentUserAccessResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<CurrentUserAccessResponse>>> GetCurrentAccess()
    {
        ApiResponse<UserAccount> loginUserResult;

        try
        {
            loginUserResult = await GetLoginUserAsync();
        }
        catch
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<CurrentUserAccessResponse>.Fail(
                    AuthErrorCode.DatabaseError,
                    "현재 사용자 정보를 조회하는 중 데이터베이스 오류가 발생했습니다."));
        }

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Unauthorized(ApiResponse<CurrentUserAccessResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _currentUserAccessService.GetCurrentAccessAsync(
            loginUserResult.Data);

        if (!result.Success)
        {
            if (result.ErrorCode == AuthErrorCode.DatabaseError)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, result);
            }

            return Unauthorized(result);
        }

        return Ok(result);
    }

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

    private async Task<ApiResponse<UserAccount>> GetLoginUserAsync()
    {
        var authorizationHeader = Request.Headers.Authorization.FirstOrDefault();

        return await _accountService.GetLoginUserByTokenAsync(authorizationHeader);
    }

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
