using Microsoft.AspNetCore.Mvc;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.Config;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Services;

namespace poscam.AuthServer.Controllers;

/// <summary>
/// 관리자/담당자용 설정 조회 Controller.
/// 
/// 관리자 페이지에서는 NVR/채널 설정을 조회만 할 수 있다.
/// 설정 수정/저장은 현장 캠뷰어에서만 가능하다.
/// </summary>
[ApiController]
public class ConfigManageController : ControllerBase
{
    private readonly ConfigManageService _configManageService;
    private readonly AccountService _accountService;

    public ConfigManageController(
        ConfigManageService configManageService,
        AccountService accountService)
    {
        _configManageService = configManageService;
        _accountService = accountService;
    }

    /// <summary>
    /// 매장 설정 조회 API.
    /// 
    /// 관리자:
    /// - 전체 매장 조회 가능
    /// 
    /// 담당자:
    /// - 본인에게 배정된 매장만 조회 가능
    /// 
    /// 주의:
    /// - 조회 전용 API다.
    /// - NVR 비밀번호는 반환하지 않는다.
    /// </summary>
    [HttpGet("api/manage/stores/{storeCode:int}/config")]
    [ProducesResponseType(typeof(ApiResponse<ManageConfigResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ManageConfigResponse>>> GetStoreConfig(
        int storeCode)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<ManageConfigResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _configManageService.GetStoreConfigAsync(
            storeCode,
            loginUserResult.Data);

        return Ok(result);
    }

    private async Task<ApiResponse<UserAccount>> GetLoginUserAsync()
    {
        var authorizationHeader = Request.Headers.Authorization.FirstOrDefault();

        return await _accountService.GetLoginUserByTokenAsync(authorizationHeader);
    }
}