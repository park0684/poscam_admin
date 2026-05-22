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
/// 관리자:
/// - 전체 매장 조회 가능
/// 
/// 담당자:
/// - 본인에게 배정된 매장만 조회 가능
/// </summary>
[ApiController]
[Route("api/manage/stores")]
public class StoreManageController : ControllerBase
{
    private readonly StoreManageService _storeManageService;
    private readonly AccountService _accountService;

    public StoreManageController(
        StoreManageService storeManageService,
        AccountService accountService)
    {
        _storeManageService = storeManageService;
        _accountService = accountService;
    }

    /// <summary>
    /// 매장 목록 조회 API.
    /// 
    /// Authorization 헤더의 관리자/담당자 토큰으로 로그인 사용자를 확인한다.
    /// 관리자는 전체 매장을 조회하고,
    /// 담당자는 본인에게 배정된 매장만 조회한다.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<StoreListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<StoreListItemDto>>>> GetStores()
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<List<StoreListItemDto>>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var loginUser = loginUserResult.Data;

        var result = await _storeManageService.GetStoreListAsync(
        loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 매장 상세 조회 API.
    /// 
    /// 매장 기본정보, 담당자 연결, 계약, 라이선스,
    /// PC캠/캠뷰어 장비, NVR 설정, 채널 설정을 한 번에 조회한다.
    /// </summary>
    [HttpGet("{storeCode:int}/detail")]
    [ProducesResponseType(typeof(ApiResponse<StoreDetailResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<StoreDetailResponse>>> GetStoreDetail(
        int storeCode)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<StoreDetailResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var loginUser = loginUserResult.Data;

        var result = await _storeManageService.GetStoreDetailAsync(
            storeCode,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 매장 등록/수정 API.
    /// 
    /// StoreCode가 없거나 0이면 신규 등록,
    /// StoreCode가 있으면 기존 매장 수정으로 처리한다.
    /// 
    /// 신규 등록 시 매장 ID와 최초 비밀번호는 백엔드에서 자동 생성된다.
    /// </summary>
    [HttpPost("save")]
    [ProducesResponseType(typeof(ApiResponse<StoreSaveResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<StoreSaveResponse>>> SaveStore(
    [FromBody] StoreSaveRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<StoreSaveResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var loginUser = loginUserResult.Data;

        var result = await _storeManageService.SaveStoreAsync(
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 매장 담당자 연결 API.
    /// 
    /// 특정 매장에 담당자와 역할을 연결한다.
    /// 연결된 담당자는 해당 매장을 조회할 수 있다.
    /// </summary>
    [HttpPost("{storeCode:int}/assignments")]
    [ProducesResponseType(typeof(ApiResponse<StoreAssignmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<StoreAssignmentResponse>>> AddAssignment(
        int storeCode,
        [FromBody] StoreAssignmentCreateRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

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
    /// 
    /// 물리 삭제하지 않고 store_user_assignments.status를 Released로 변경한다.
    /// </summary>
    [HttpDelete("assignments/{assignmentCode:int}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> ReleaseAssignment(
    int assignmentCode)
    {
        var loginUserResult = await GetLoginUserAsync();

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
    /// Authorization 헤더의 Bearer 토큰으로 로그인 사용자를 확인한다.
    /// </summary>
    private async Task<ApiResponse<UserAccount>> GetLoginUserAsync()
    {
        var authorizationHeader = Request.Headers.Authorization.FirstOrDefault();

        return await _accountService.GetLoginUserByTokenAsync(authorizationHeader);
    }

    [HttpGet("{storeCode:int}/usage-summary")]
    [ProducesResponseType(typeof(ApiResponse<StoreUsageSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<StoreUsageSummaryDto>>> GetUsageSummary(
    int storeCode)
    {
        var loginUserResult = await GetLoginUserAsync();

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
}