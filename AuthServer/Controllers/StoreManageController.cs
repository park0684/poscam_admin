using Microsoft.AspNetCore.Mvc;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.Store;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Services;

namespace poscam.AuthServer.Controllers;

/// <summary>
/// 관리자/담당자용 매장 관리 API Controller.
///
/// 매장 목록·상세 조회, 등록·수정,
/// 담당자 연결·해제 및 사용 현황 조회를 제공한다.
/// </summary>
[ApiController]
[Route("api/manage/stores")]
public class StoreManageController : ControllerBase
{
    private readonly StoreManageService _storeManageService;
    private readonly ConfigManageService _configManageService;
    private readonly AccountService _accountService;
    private readonly AdminPermissionService _adminPermissionService;
    private readonly PartnerUserPermissionService _partnerUserPermissionService;

    public StoreManageController(
        StoreManageService storeManageService,
        ConfigManageService configManageService,
        AccountService accountService,
        AdminPermissionService adminPermissionService,
        PartnerUserPermissionService partnerUserPermissionService)
    {
        _storeManageService = storeManageService;
        _configManageService = configManageService;
        _accountService = accountService;
        _adminPermissionService = adminPermissionService;
        _partnerUserPermissionService = partnerUserPermissionService;
    }

    /// <summary>
    /// 매장 목록 조회 API.
    ///
    /// System은 전체 조회 가능하다.
    /// Admin과 PartnerUser는 StoreManage 권한이 필요하며,
    /// PartnerUser의 실제 조회 범위는 Service에서 소속 파트너사 기준으로 제한한다.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<StoreListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<StoreListItemDto>>>> GetStores(
        [FromQuery] StoreListSearchRequest request)
    {
        var loginUserResult = await GetAuthorizedLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<List<StoreListItemDto>>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _storeManageService.GetStoreListAsync(
            loginUserResult.Data,
            request);

        return Ok(result);
    }

    /// <summary>
    /// 매장 상세 조회 API.
    ///
    /// 매장 기본정보, 담당자 연결, 계약, 라이선스,
    /// PC캠/캠뷰어 장비, NVR 설정, 채널 설정을 한 번에 조회한다.
    ///
    /// StoreManageService의 기존 NvrConfig 단일 조회는 전환기 호환용으로 남아 있으나,
    /// 최종 응답의 NVR/채널 영역은 ConfigManageService의 다중 NVR 조회 결과로 교체한다.
    /// </summary>
    [HttpGet("{storeCode:int}/detail")]
    [ProducesResponseType(typeof(ApiResponse<StoreDetailResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<StoreDetailResponse>>> GetStoreDetail(
        int storeCode)
    {
        var loginUserResult = await GetAuthorizedLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<StoreDetailResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var loginUser = loginUserResult.Data;
        var result = await _storeManageService.GetStoreDetailAsync(
            storeCode,
            loginUser);

        if (!result.Success || result.Data == null)
        {
            return Ok(result);
        }

        var configResult = await _configManageService.GetStoreConfigAsync(
            storeCode,
            loginUser);

        if (!configResult.Success || configResult.Data == null)
        {
            return Ok(ApiResponse<StoreDetailResponse>.Fail(
                configResult.ErrorCode,
                configResult.Message));
        }

        result.Data.Nvrs = configResult.Data.Nvrs;
        result.Data.NvrConfig = configResult.Data.NvrConfig;
        result.Data.ChannelConfigs = configResult.Data.Channels;

        return Ok(result);
    }

    /// <summary>
    /// 매장 등록/수정 API.
    ///
    /// StoreCode가 없거나 0이면 신규 등록,
    /// StoreCode가 있으면 기존 매장 수정으로 처리한다.
    /// 신규 등록 시 매장 ID와 최초 비밀번호는 백엔드에서 자동 생성된다.
    /// </summary>
    [HttpPost("save")]
    [ProducesResponseType(typeof(ApiResponse<StoreSaveResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<StoreSaveResponse>>> SaveStore(
        [FromBody] StoreSaveRequest request)
    {
        var loginUserResult = await GetAuthorizedLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<StoreSaveResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _storeManageService.SaveStoreAsync(
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 매장 담당자 연결 API.
    /// 특정 매장에 담당자와 역할을 연결한다.
    /// </summary>
    [HttpPost("{storeCode:int}/assignments")]
    [ProducesResponseType(typeof(ApiResponse<StoreAssignmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<StoreAssignmentResponse>>> AddAssignment(
        int storeCode,
        [FromBody] StoreAssignmentCreateRequest request)
    {
        var loginUserResult = await GetAuthorizedLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<StoreAssignmentResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var loginUser = loginUserResult.Data;

        request.StoreCode = storeCode;
        request.AssignedBy = loginUser.UserCode;

        var result = await _storeManageService.AddAssignmentAsync(
            request,
            loginUser);

        return Ok(result);
    }

    /// <summary>
    /// 매장 담당자 연결 해제 API.
    /// 물리 삭제하지 않고 연결 상태를 Released로 변경한다.
    /// </summary>
    [HttpDelete("assignments/{assignmentCode:int}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> ReleaseAssignment(
        int assignmentCode)
    {
        var loginUserResult = await GetAuthorizedLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<bool>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _storeManageService.ReleaseAssignmentAsync(
            assignmentCode,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 매장 사용 현황 조회 API.
    /// </summary>
    [HttpGet("{storeCode:int}/usage-summary")]
    [ProducesResponseType(typeof(ApiResponse<StoreUsageSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<StoreUsageSummaryDto>>> GetUsageSummary(
        int storeCode)
    {
        var loginUserResult = await GetAuthorizedLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<StoreUsageSummaryDto>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _storeManageService.GetUsageSummaryAsync(
            storeCode,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 로그인 확인 후 역할에 맞는 StoreManage 권한을 검사한다.
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
                AdminPermissionType.StoreManage);
        }
        else if (loginUserRole == UserRole.PartnerUser)
        {
            permissionResult = await _partnerUserPermissionService.CheckPermissionAsync(
                loginUser,
                PartnerUserPermissionType.StoreManage);
        }
        else
        {
            return ApiResponse<UserAccount>.Fail(
                AuthErrorCode.PermissionDenied,
                "매장 관리 기능을 사용할 권한이 없습니다.");
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
