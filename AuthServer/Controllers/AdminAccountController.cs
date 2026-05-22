using Microsoft.AspNetCore.Mvc;
using poscam.AuthServer.Models.Dtos.Admin;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.UserManage;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Services;

namespace poscam.AuthServer.Controllers;

/// <summary>
/// 관리자 계정 관리 API Controller.
/// 
/// 관리자 계정 목록 조회, 상세 조회, 생성, 수정,
/// 비밀번호 초기화, 권한 조회/수정을 담당한다.
/// 실제 세부 권한 검증은 AdminAccountManageService에서 처리한다.
/// </summary>
[ApiController]
[Route("api/admin/accounts")]
public class AdminAccountController : ControllerBase
{
    private readonly AdminAccountManageService _adminAccountManageService;
    private readonly AccountService _accountService;

    public AdminAccountController(
        AdminAccountManageService adminAccountManageService,
        AccountService accountService)
    {
        _adminAccountManageService = adminAccountManageService;
        _accountService = accountService;
    }

    /// <summary>
    /// 관리자 계정 목록 조회 API.
    /// 
    /// System은 전체 조회 가능하고,
    /// Admin은 AdminAccountManage 권한이 있어야 조회할 수 있다.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<UserManageListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<UserManageListItemDto>>>> GetAdminAccounts(
        [FromQuery] int? userStatus)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<List<UserManageListItemDto>>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _adminAccountManageService.GetAdminAccountsAsync(
            userStatus,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 관리자 계정 상세 조회 API.
    /// </summary>
    [HttpGet("{userCode:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserManageDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserManageDetailDto>>> GetAdminAccountDetail(
        int userCode)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<UserManageDetailDto>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _adminAccountManageService.GetAdminAccountDetailAsync(
            userCode,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 관리자 계정 생성 API.
    /// 
    /// AdminAccountManage 권한이 필요하다.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserSaveResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserSaveResponse>>> CreateAdminAccount(
        [FromBody] AdminAccountCreateRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<UserSaveResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _adminAccountManageService.CreateAdminAccountAsync(
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 관리자 계정 기본정보 수정 API.
    /// 
    /// AdminAccountManage 권한이 필요하다.
    /// 비밀번호와 권한은 별도 API에서 처리한다.
    /// </summary>
    [HttpPut("{userCode:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserSaveResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserSaveResponse>>> UpdateAdminAccount(
        int userCode,
        [FromBody] AdminAccountUpdateRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<UserSaveResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        request.UserCode = userCode;

        var result = await _adminAccountManageService.UpdateAdminAccountAsync(
            userCode,
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 관리자 계정 비밀번호 초기화 API.
    /// 
    /// AdminPasswordReset 권한이 필요하다.
    /// </summary>
    [HttpPut("{userCode:int}/password")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> ResetAdminPassword(
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

        var result = await _adminAccountManageService.ResetAdminPasswordAsync(
            userCode,
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 관리자 계정 권한 목록 조회 API.
    /// 
    /// AdminPermissionManage 권한이 필요하다.
    /// </summary>
    [HttpGet("{userCode:int}/permissions")]
    [ProducesResponseType(typeof(ApiResponse<List<int>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<int>>>> GetAdminPermissions(
        int userCode)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<List<int>>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _adminAccountManageService.GetAdminPermissionsAsync(
            userCode,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 관리자 계정 권한 수정 API.
    /// 
    /// AdminPermissionManage 권한이 필요하다.
    /// 전달된 권한 코드 목록으로 기존 권한을 교체한다.
    /// </summary>
    [HttpPut("{userCode:int}/permissions")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateAdminPermissions(
        int userCode,
        [FromBody] AdminAccountPermissionUpdateRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<bool>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        request.UserCode = userCode;

        var result = await _adminAccountManageService.UpdateAdminPermissionsAsync(
            userCode,
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// Authorization 헤더의 Bearer 토큰으로 로그인 사용자를 확인한다.
    /// </summary>
    private async Task<ApiResponse<UserAccount>> GetLoginUserAsync()
    {
        var authorizationHeader = Request.Headers.Authorization.FirstOrDefault();

        return await _accountService.GetLoginUserByTokenAsync(
            authorizationHeader);
    }
}