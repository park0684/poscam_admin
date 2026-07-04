using Microsoft.AspNetCore.Mvc;
using poscam.AuthServer.Models.Dtos.Admin;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.License;
using poscam.AuthServer.Models.Dtos.Store;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Repositories;
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
    private readonly AdminPermissionService _adminPermissionService;
    private readonly PartnerUserPermissionService _partnerUserPermissionService;
    private readonly ContractRepository _contractRepository;
    private readonly StoreRepository _storeRepository;

    public LicenseManageController(
        LicenseManageService licenseManageService,
        AccountService accountService,
        AdminPermissionService adminPermissionService,
        PartnerUserPermissionService partnerUserPermissionService,
        ContractRepository contractRepository,
        StoreRepository storeRepository)
    {
        _licenseManageService = licenseManageService;
        _accountService = accountService;
        _adminPermissionService = adminPermissionService;
        _partnerUserPermissionService = partnerUserPermissionService;
        _contractRepository = contractRepository;
        _storeRepository = storeRepository;
    }

    /// <summary>
    /// 매장 기준 라이선스 목록 조회 API.
    ///
    /// System은 전체 조회 가능하다.
    /// Admin과 PartnerUser는 LicenseManage 권한이 필요하며,
    /// PartnerUser의 실제 매장 접근 범위는 Service에서 추가 확인한다.
    /// </summary>
    [HttpGet("api/manage/stores/{storeCode:int}/licenses")]
    [ProducesResponseType(typeof(ApiResponse<List<StoreLicenseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<StoreLicenseDto>>>> GetLicensesByStore(
        int storeCode)
    {
        var loginUserResult = await GetAuthorizedLoginUserAsync();

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
    /// 라이선스 발급 화면에서 사용할 매장 기준 계약 목록 조회 API.
    ///
    /// ContractManage 권한이 아니라 LicenseManage 권한으로 접근한다.
    /// 실제 계약 등록/수정 권한은 기존 계약 관리 API에서 별도로 검증한다.
    /// </summary>
    [HttpGet("api/manage/stores/{storeCode:int}/license-contracts")]
    [ProducesResponseType(typeof(ApiResponse<List<StoreContractDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<StoreContractDto>>>> GetIssueContractsByStore(
        int storeCode)
    {
        var loginUserResult = await GetAuthorizedLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<List<StoreContractDto>>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        if (storeCode <= 0)
        {
            return Ok(ApiResponse<List<StoreContractDto>>.Fail(
                AuthErrorCode.InvalidStore,
                "매장 코드가 올바르지 않습니다."));
        }

        var loginUser = loginUserResult.Data;
        var loginUserRole = (UserRole)loginUser.UserRole;

        if (loginUserRole == UserRole.PartnerUser)
        {
            if (!loginUser.PartnerCode.HasValue || loginUser.PartnerCode.Value <= 0)
            {
                return Ok(ApiResponse<List<StoreContractDto>>.Fail(
                    AuthErrorCode.PermissionDenied,
                    "담당자 계정에 소속 파트너 정보가 없습니다."));
            }

            var canAccessStore = await _storeRepository.CanPartnerAccessStoreAsync(
                loginUser.PartnerCode.Value,
                storeCode);

            if (!canAccessStore)
            {
                return Ok(ApiResponse<List<StoreContractDto>>.Fail(
                    AuthErrorCode.PermissionDenied,
                    "해당 매장의 라이선스 발급용 계약 목록을 조회할 권한이 없습니다."));
            }
        }
        else if (loginUserRole != UserRole.System &&
                 loginUserRole != UserRole.Admin)
        {
            return Ok(ApiResponse<List<StoreContractDto>>.Fail(
                AuthErrorCode.PermissionDenied,
                "라이선스 발급용 계약 목록을 조회할 권한이 없습니다."));
        }

        var contracts = await _contractRepository.GetByStoreAsync(storeCode);

        return Ok(ApiResponse<List<StoreContractDto>>.Ok(
            contracts,
            "라이선스 발급용 계약 목록을 조회했습니다."));
    }

    /// <summary>
    /// 계약 기준 라이선스 목록 조회 API.
    /// </summary>
    [HttpGet("api/manage/contracts/{contractCode:int}/licenses")]
    [ProducesResponseType(typeof(ApiResponse<List<StoreLicenseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<StoreLicenseDto>>>> GetLicensesByContract(
        int contractCode)
    {
        var loginUserResult = await GetAuthorizedLoginUserAsync();

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
    /// 계약의 PC캠 허용 수량을 초과해서 발급할 수 없다.
    /// </summary>
    [HttpPost("api/manage/contracts/{contractCode:int}/licenses/issue")]
    [ProducesResponseType(typeof(ApiResponse<LicenseIssueResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LicenseIssueResponse>>> IssueLicenses(
        int contractCode,
        [FromBody] LicenseIssueManageRequest request)
    {
        var loginUserResult = await GetAuthorizedLoginUserAsync();

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
    /// 인증키 폐기 API.
    /// 폐기된 인증키는 이후 PC캠 인증에 사용할 수 없다.
    /// </summary>
    [HttpPost("api/manage/licenses/{licenseCode:int}/revoke")]
    [ProducesResponseType(typeof(ApiResponse<LicenseRevokeResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LicenseRevokeResponse>>> RevokeLicense(
        int licenseCode,
        [FromBody] LicenseRevokeManageRequest request)
    {
        var loginUserResult = await GetAuthorizedLoginUserAsync();

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
    /// 연결된 디바이스가 있으면 사용중 상태로,
    /// 연결된 디바이스가 없으면 초기화 상태로 복구한다.
    /// </summary>
    [HttpPost("api/manage/licenses/{licenseCode:int}/restore")]
    [ProducesResponseType(typeof(ApiResponse<LicenseRestoreResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LicenseRestoreResponse>>> RestoreLicense(
        int licenseCode,
        [FromBody] LicenseRestoreManageRequest request)
    {
        var loginUserResult = await GetAuthorizedLoginUserAsync();

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

    /// <summary>
    /// 로그인 확인 후 역할에 맞는 LicenseManage 권한을 검사한다.
    /// </summary>
    private async Task<ApiResponse<UserAccount>> GetAuthorizedLoginUserAsync()
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return loginUserResult;
        }

        var loginUser = loginUserResult.Data;
        var loginUserRole = (UserRole)loginUser.UserRole;

        ApiResponse<bool> permissionResult;

        if (loginUserRole == UserRole.System ||
            loginUserRole == UserRole.Admin)
        {
            permissionResult = await _adminPermissionService.CheckPermissionAsync(
                loginUser,
                AdminPermissionType.LicenseManage);
        }
        else if (loginUserRole == UserRole.PartnerUser)
        {
            permissionResult = await _partnerUserPermissionService.CheckPermissionAsync(
                loginUser,
                PartnerUserPermissionType.LicenseManage);
        }
        else
        {
            return ApiResponse<UserAccount>.Fail(
                AuthErrorCode.PermissionDenied,
                "라이선스 관리 기능을 사용할 권한이 없습니다.");
        }

        if (!permissionResult.Success)
        {
            return ApiResponse<UserAccount>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        return ApiResponse<UserAccount>.Ok(loginUser);
    }

    /// <summary>
    /// Authorization 헤더의 Bearer 토큰으로 로그인 사용자를 확인한다.
    /// </summary>
    private async Task<ApiResponse<UserAccount>> GetLoginUserAsync()
    {
        var authorizationHeader = Request.Headers.Authorization.FirstOrDefault();

        return await _accountService.GetLoginUserByTokenAsync(authorizationHeader);
    }
}
