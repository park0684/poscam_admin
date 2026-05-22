using Microsoft.AspNetCore.Mvc;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.Contract;
using poscam.AuthServer.Models.Dtos.Store;
using poscam.AuthServer.Models.Entities;
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

    public ContractManageController(
        ContractManageService contractManageService,
        AccountService accountService)
    {
        _contractManageService = contractManageService;
        _accountService = accountService;
    }

    /// <summary>
    /// 매장별 계약 목록 조회 API.
    /// 
    /// 관리자는 모든 매장의 계약을 조회할 수 있고,
    /// 담당자는 본인에게 배정된 매장의 계약만 조회할 수 있다.
    /// </summary>
    [HttpGet("api/manage/stores/{storeCode:int}/contracts")]
    [ProducesResponseType(typeof(ApiResponse<List<StoreContractDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<StoreContractDto>>>> GetContractsByStore(
        int storeCode)
    {
        var loginUserResult = await GetLoginUserAsync();

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
    /// 
    /// 관리자만 호출할 수 있다.
    /// storeCode는 Route 값을 우선 사용한다.
    /// </summary>
    [HttpPost("api/manage/stores/{storeCode:int}/contracts")]
    [ProducesResponseType(typeof(ApiResponse<ContractSaveResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ContractSaveResponse>>> CreateContract(
        int storeCode,
        [FromBody] ContractSaveRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

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
    /// 
    /// 관리자만 호출할 수 있다.
    /// </summary>
    [HttpPut("api/manage/contracts/{contractCode:int}")]
    [ProducesResponseType(typeof(ApiResponse<ContractSaveResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ContractSaveResponse>>> UpdateContract(
        int contractCode,
        [FromBody] ContractSaveRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

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
    /// Authorization 헤더의 Bearer 토큰으로 로그인 사용자를 확인한다.
    /// </summary>
    private async Task<ApiResponse<UserAccount>> GetLoginUserAsync()
    {
        var authorizationHeader = Request.Headers.Authorization.FirstOrDefault();

        return await _accountService.GetLoginUserByTokenAsync(authorizationHeader);
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
        var loginUserResult = await GetLoginUserAsync();

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
}