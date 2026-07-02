using Microsoft.AspNetCore.Mvc;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.Device;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Services;

namespace poscam.AuthServer.Controllers;

/// <summary>
/// 장비 관리 API Controller.
///
/// 관리자/담당자 계정 토큰 기반으로 PC캠 및 캠뷰어 장비 초기화를 처리한다.
/// </summary>
[ApiController]
[Route("api/devices")]
public class DeviceController : ControllerBase
{
    private readonly DeviceService _deviceService;
    private readonly AccountService _accountService;
    private readonly AdminPermissionService _adminPermissionService;
    private readonly PartnerUserPermissionService _partnerUserPermissionService;

    public DeviceController(
        DeviceService deviceService,
        AccountService accountService,
        AdminPermissionService adminPermissionService,
        PartnerUserPermissionService partnerUserPermissionService)
    {
        _deviceService = deviceService;
        _accountService = accountService;
        _adminPermissionService = adminPermissionService;
        _partnerUserPermissionService = partnerUserPermissionService;
    }

    /// <summary>
    /// 장비 초기화 API.
    ///
    /// 권한 정책:
    /// - System: 모든 장비 초기화 가능
    /// - Admin: DeviceManage 권한이 있어야 초기화 가능
    /// - PartnerUser: DeviceManage 권한과 DeviceService의 매장 접근 범위를 모두 만족해야 가능
    ///
    /// 일반 사용자는 이 API를 사용할 수 없다.
    /// 일반 사용자의 캠뷰어 장비 해제는 /api/viewer/devices/release를 사용한다.
    /// </summary>
    [HttpPost("reset")]
    [ProducesResponseType(typeof(ApiResponse<DeviceResetResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DeviceResetResponse>>> ResetDevice(
        [FromBody] DeviceResetRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<DeviceResetResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var loginUser = loginUserResult.Data;
        var loginUserRole = (UserRole)loginUser.UserRole;

        ApiResponse<bool> permissionResult;

        if (loginUserRole == UserRole.System ||
            loginUserRole == UserRole.Admin)
        {
            permissionResult = await _adminPermissionService.CheckPermissionAsync(
                loginUser,
                AdminPermissionType.DeviceManage);
        }
        else if (loginUserRole == UserRole.PartnerUser)
        {
            permissionResult = await _partnerUserPermissionService.CheckPermissionAsync(
                loginUser,
                PartnerUserPermissionType.DeviceManage);
        }
        else
        {
            return Ok(ApiResponse<DeviceResetResponse>.Fail(
                AuthErrorCode.PermissionDenied,
                "장비 관리 기능을 사용할 권한이 없습니다."));
        }

        if (!permissionResult.Success)
        {
            return Ok(ApiResponse<DeviceResetResponse>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message));
        }

        var result = await _deviceService.ResetDeviceAsync(
            request,
            loginUser,
            GetClientIp());

        return Ok(result);
    }

    private async Task<ApiResponse<UserAccount>> GetLoginUserAsync()
    {
        var authorizationHeader = Request.Headers.Authorization.FirstOrDefault();

        return await _accountService.GetLoginUserByTokenAsync(authorizationHeader);
    }

    private string? GetClientIp()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
