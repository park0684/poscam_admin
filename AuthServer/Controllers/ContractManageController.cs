using Microsoft.AspNetCore.Mvc;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.Contract;
using poscam.AuthServer.Models.Dtos.Store;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Services;

namespace poscam.AuthServer.Controllers;

/// <summary>
/// 관리자/담당자용 계약 관리 API Controller.
///
/// 계약 목록 조회, 계약 등록, 계약 수정을 담당한다.
/// </summary>
[ApiController]
public class ContractManageController : ControllerBase
{
    private readonly ContractManageService _contractManageService;
    private readonly AccountService _accountService;
    private readonly AdminPermissionService _adminPermissionService;
    private readonly PartnerUserPermissionService _partnerUserPermissionService;

    public ContractManageController(
        ContractManageService contractManageService,
        AccountService accountService,
        AdminPermissionService adminPermissionService,
        PartnerUserPermissionService partnerUserPermissionService)
    {
        _contractManageService = contractManageService;
        _accountService = accountService;
        _adminPermissionService = adminPermissionService;
        _partnerUserPermissionService = partnerUserPermissionService;
    }

    /// <summary>
    /// 매장별 계약 목록 조회 API.
    ///
    /// System은 전체 조회 가능하다.
    /// Admin과 PartnerUser는 ContractManage 권한이 필요하며,
    /// PartnerUser의 실제 매장 접근 범위는 Service에서 추가 확인한다.
    ///
    /// 단, 라이선스 발급 화면에서 계약 선택 목록을 불러오기 위해
    /// PartnerUser는 LicenseManage 권한만 있어도 조회를 허용한다.
    /// 계약 등록/수정 API는 기존처럼 ContractManage 권한을 요구한다.
    /// </summary>
    [HttpGet("api/manage/stores/{storeCode:int}/contracts")]
    [ProducesResponseType(typeof(ApiResponse<List<StoreContractDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<StoreContractDto>>>> GetContractsByStore(
        int storeCode)
    {
        var loginUserResult = await GetAuthorizedContractListUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<List<StoreContractDto>>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _contractManageService.GetContractsByStoreAsync(
            storeCode,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 신규 계약 등록 API.
    /// storeCode는 Route 값을 우선 사용한다.
    /// </summary>
    [HttpPost("api/manage/stores/{storeCode:int}/contracts")]
    [ProducesResponseType(typeof(ApiResponse<ContractSaveResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ContractSaveResponse>>> CreateContract(
        int storeCode,
        [FromBody] ContractSaveRequest request)
    {
        var loginUserResult = await GetAuthorizedLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<ContractSaveResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        request.StoreCode = storeCode;
        request.ContractCode = null;

        var result = await _contractManageService.SaveContractAsync(
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 기존 계약 수정 API.
    /// </summary>
    [HttpPut("api/manage/contracts/{contractCode:int}")]
    [ProducesResponseType(typeof(ApiResponse<ContractSaveResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ContractSaveResponse>>> UpdateContract(
        int contractCode,
        [FromBody] ContractSaveRequest request)
    {
        var loginUserResult = await GetAuthorizedLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<ContractSaveResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        request.ContractCode = contractCode;

        var result = await _contractManageService.SaveContractAsync(
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 파트너사 기준 신규 계약 등록 API.
    ///
    /// 매장과 연결되지 않은 계약을 생성한다.
    /// 계약의 소유 파트너사는 Route의 partnerCode를 사용한다.
    /// </summary>
    [HttpPost("api/manage/partners/{partnerCode:int}/contracts")]
    [ProducesResponseType(typeof(ApiResponse<ContractSaveResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ContractSaveResponse>>> CreatePartnerContract(
        int partnerCode,
        [FromBody] PartnerContractSaveRequest request)
    {
        var loginUserResult = await GetAuthorizedLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<ContractSaveResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _contractManageService.CreatePartnerContractAsync(
            partnerCode,
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 계약 목록 조회용 로그인 확인.
    ///
    /// 기존 계약 관리 권한 외에, PartnerUser의 LicenseManage 권한도 허용한다.
    /// 라이선스 발급 화면에서 계약 선택 목록을 조회하기 위한 예외이다.
    /// </summary>
    private async Task<ApiResponse<UserAccount>> GetAuthorizedContractListUserAsync()
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return loginUserResult;
        }

        var loginUser = loginUserResult.Data;
        var loginUserRole = (UserRole)loginUser.UserRole;

        if (loginUserRole == UserRole.System)
        {
            return ApiResponse<UserAccount>.Ok(loginUser);
        }

        if (loginUserRole == UserRole.Admin)
        {
            var permissionResult = await _adminPermissionService.CheckAnyPermissionAsync(
                loginUser,
                AdminPermissionType.ContractManage,
                AdminPermissionType.LicenseManage);

            if (!permissionResult.Success)
            {
                return ApiResponse<UserAccount>.Fail(
                    permissionResult.ErrorCode,
                    permissionResult.Message);
            }

            return ApiResponse<UserAccount>.Ok(loginUser);
        }

        if (loginUserRole == UserRole.PartnerUser)
        {
            var contractPermissionResult = await _partnerUserPermissionService.CheckPermissionAsync(
                loginUser,
                PartnerUserPermissionType.ContractManage);

            if (contractPermissionResult.Success)
            {
                return ApiResponse<UserAccount>.Ok(loginUser);
            }

            var licensePermissionResult = await _partnerUserPermissionService.CheckPermissionAsync(
                loginUser,
                PartnerUserPermissionType.LicenseManage);

            if (!licensePermissionResult.Success)
            {
                return ApiResponse<UserAccount>.Fail(
                    licensePermissionResult.ErrorCode,
                    licensePermissionResult.Message);
            }

            return ApiResponse<UserAccount>.Ok(loginUser);
        }

        return ApiResponse<UserAccount>.Fail(
            AuthErrorCode.PermissionDenied,
            "계약 목록을 조회할 권한이 없습니다.");
    }

    /// <summary>
    /// 로그인 확인 후 역할에 맞는 ContractManage 권한을 검사한다.
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
                AdminPermissionType.ContractManage);
        }
        else if (loginUserRole == UserRole.PartnerUser)
        {
            permissionResult = await _partnerUserPermissionService.CheckPermissionAsync(
                loginUser,
                PartnerUserPermissionType.ContractManage);
        }
        else
        {
            return ApiResponse<UserAccount>.Fail(
                AuthErrorCode.PermissionDenied,
                "계약 관리 기능을 사용할 권한이 없습니다.");
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
