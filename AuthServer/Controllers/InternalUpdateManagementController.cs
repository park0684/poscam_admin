using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using poscam.AuthServer.Models.Dtos.Account;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Options;
using poscam.AuthServer.Services;

namespace poscam.AuthServer.Controllers;

/// <summary>
/// UpdateServer 전용 내부 관리자 권한 확인 API.
/// </summary>
[ApiController]
[Route("api/internal/update-management")]
public class InternalUpdateManagementController : ControllerBase
{
    private const string ServiceKeyHeaderName = "X-POSCAM-Service-Key";

    private readonly Func<string?, Task<ApiResponse<UserAccount>>> _getLoginUserAsync;
    private readonly Func<UserAccount, AdminPermissionType, Task<ApiResponse<bool>>> _checkPermissionAsync;
    private readonly AuthPolicyOptions _authPolicyOptions;

    public InternalUpdateManagementController(
        AccountService accountService,
        AdminPermissionService adminPermissionService,
        IOptions<AuthPolicyOptions> authPolicyOptions)
        : this(
            authPolicyOptions,
            accountService.GetLoginUserByTokenAsync,
            adminPermissionService.CheckPermissionAsync)
    {
    }

    protected InternalUpdateManagementController(
        IOptions<AuthPolicyOptions> authPolicyOptions,
        Func<string?, Task<ApiResponse<UserAccount>>> getLoginUserAsync,
        Func<UserAccount, AdminPermissionType, Task<ApiResponse<bool>>> checkPermissionAsync)
    {
        _authPolicyOptions = authPolicyOptions.Value;
        _getLoginUserAsync = getLoginUserAsync;
        _checkPermissionAsync = checkPermissionAsync;
    }

    /// <summary>
    /// 현재 Bearer 사용자가 UpdateServer 관리 기능을 사용할 수 있는지 확인한다.
    /// </summary>
    [HttpPost("authorize")]
    [ProducesResponseType(typeof(ApiResponse<UpdateManagementActorResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UpdateManagementActorResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<UpdateManagementActorResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<UpdateManagementActorResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<UpdateManagementActorResponse>>> Authorize(
        [FromHeader(Name = ServiceKeyHeaderName)] string? serviceKey)
    {
        if (!FixedTimeSecretComparer.MatchesConfiguredSecret(
                serviceKey,
                _authPolicyOptions.InternalServiceKey,
                AuthPolicyOptions.InternalServiceKeyPlaceholder))
        {
            return Unauthorized(ApiResponse<UpdateManagementActorResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "내부 서비스 인증에 실패했습니다."));
        }

        ApiResponse<UserAccount> loginUserResult;

        try
        {
            var authorizationHeader = Request.Headers.Authorization.FirstOrDefault();
            loginUserResult = await _getLoginUserAsync(authorizationHeader);
        }
        catch
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<UpdateManagementActorResponse>.Fail(
                    AuthErrorCode.DatabaseError,
                    "현재 사용자 정보를 확인하는 중 데이터베이스 오류가 발생했습니다."));
        }

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Unauthorized(ApiResponse<UpdateManagementActorResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        ApiResponse<UpdateManagementActorResponse> authorizationResult;

        try
        {
            authorizationResult = await UpdateManagementAuthorizationHelper.AuthorizeActorAsync(
                loginUserResult.Data,
                _checkPermissionAsync);
        }
        catch
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<UpdateManagementActorResponse>.Fail(
                    AuthErrorCode.DatabaseError,
                    "업데이트 관리 권한을 확인하는 중 데이터베이스 오류가 발생했습니다."));
        }

        if (!authorizationResult.Success)
        {
            return StatusCode(StatusCodes.Status403Forbidden, authorizationResult);
        }

        return Ok(authorizationResult);
    }
}
