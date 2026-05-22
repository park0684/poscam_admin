using Microsoft.AspNetCore.Mvc;
using poscam.AuthServer.Models.Dtos.Admin;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.License;
using poscam.AuthServer.Models.Dtos.Store;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Services;

namespace poscam.AuthServer.Controllers;

/// <summary>
/// 관리자/담당자용 라이선스 관리 API Controller.
/// 
/// 라이선스 목록 조회와 PC캠 라이선스 발급 기능을 제공한다.
/// </summary>
[ApiController]
public class LicenseManageController : ControllerBase
{
    private readonly LicenseManageService _licenseManageService;
    private readonly AccountService _accountService;

    public LicenseManageController(
        LicenseManageService licenseManageService,
        AccountService accountService)
    {
        _licenseManageService = licenseManageService;
        _accountService = accountService;
    }

    /// <summary>
    /// 매장 기준 라이선스 목록 조회 API.
    /// 
    /// 관리자:
    /// - 모든 매장 조회 가능
    /// 
    /// 담당자:
    /// - 본인에게 배정된 매장만 조회 가능
    /// </summary>
    [HttpGet("api/manage/stores/{storeCode:int}/licenses")]
    [ProducesResponseType(typeof(ApiResponse<List<StoreLicenseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<StoreLicenseDto>>>> GetLicensesByStore(
        int storeCode)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<List<StoreLicenseDto>>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _licenseManageService.GetLicensesByStoreAsync(
            storeCode,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 계약 기준 라이선스 목록 조회 API.
    /// </summary>
    [HttpGet("api/manage/contracts/{contractCode:int}/licenses")]
    [ProducesResponseType(typeof(ApiResponse<List<StoreLicenseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<StoreLicenseDto>>>> GetLicensesByContract(
        int contractCode)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<List<StoreLicenseDto>>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _licenseManageService.GetLicensesByContractAsync(
            contractCode,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 계약 기준 PC캠 라이선스 발급 API.
    /// 
    /// 관리자만 호출할 수 있다.
    /// 계약의 PC캠 허용 수량을 초과해서 발급할 수 없다.
    /// </summary>
    [HttpPost("api/manage/contracts/{contractCode:int}/licenses/issue")]
    [ProducesResponseType(typeof(ApiResponse<LicenseIssueResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LicenseIssueResponse>>> IssueLicenses(
        int contractCode,
        [FromBody] LicenseIssueManageRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<LicenseIssueResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _licenseManageService.IssueLicensesAsync(
            contractCode,
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

        return await _accountService.GetLoginUserByTokenAsync(authorizationHeader);
    }

    /// <summary>
    /// 인증키 폐기 API.
    /// 
    /// 관리자 또는 해당 계약의 파트너 담당자가 호출할 수 있다.
    /// 폐기된 인증키는 이후 PC캠 인증에 사용할 수 없다.
    /// </summary>
    [HttpPost("api/manage/licenses/{licenseCode:int}/revoke")]
    [ProducesResponseType(typeof(ApiResponse<LicenseRevokeResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LicenseRevokeResponse>>> RevokeLicense(
        int licenseCode,
        [FromBody] LicenseRevokeManageRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<LicenseRevokeResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _licenseManageService.RevokeLicenseAsync(
            licenseCode,
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 폐기된 인증키 복구 API.
    /// 
    /// 연결된 디바이스가 있으면 사용중 상태로,
    /// 연결된 디바이스가 없으면 초기화 상태로 복구한다.
    /// </summary>
    [HttpPost("api/manage/licenses/{licenseCode:int}/restore")]
    [ProducesResponseType(typeof(ApiResponse<LicenseRestoreResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LicenseRestoreResponse>>> RestoreLicense(
        int licenseCode,
        [FromBody] LicenseRestoreManageRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<LicenseRestoreResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _licenseManageService.RestoreLicenseAsync(
            licenseCode,
            request,
            loginUserResult.Data);

        return Ok(result);
    }
}