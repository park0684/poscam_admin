using Microsoft.AspNetCore.Mvc;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.Partner;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Services;

namespace poscam.AuthServer.Controllers;

/// <summary>
/// 관리자용 파트너사 관리 API Controller.
/// 
/// 파트너사 등록, 수정, 목록 조회, 상세 조회 기능을 제공한다.
/// 실제 권한 검증은 PartnerService에서 처리한다.
/// </summary>
[ApiController]
[Route("api/admin/partners")]
public class PartnerController : ControllerBase
{
    private readonly PartnerService _partnerService;
    private readonly AccountService _accountService;

    public PartnerController(
        PartnerService partnerService,
        AccountService accountService)
    {
        _partnerService = partnerService;
        _accountService = accountService;
    }

    /// <summary>
    /// 파트너사 목록 조회 API.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PartnerListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<PartnerListItemDto>>>> GetPartners()
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<List<PartnerListItemDto>>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var loginUser = loginUserResult.Data!;

        var result = await _partnerService.GetListAsync(loginUser);

        return Ok(result);
    }

    /// <summary>
    /// 파트너사 상세 조회 API.
    /// </summary>
    [HttpGet("{partnerCode:int}")]
    [ProducesResponseType(typeof(ApiResponse<PartnerDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PartnerDetailDto>>> GetPartnerDetail(
        int partnerCode)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<PartnerDetailDto>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var loginUser = loginUserResult.Data!;

        var result = await _partnerService.GetDetailAsync(
            partnerCode,
            loginUser);

        return Ok(result);
    }

    /// <summary>
    /// 파트너사 등록 API.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PartnerSaveResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PartnerSaveResponse>>> CreatePartner(
        [FromBody] PartnerCreateRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<PartnerSaveResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var loginUser = loginUserResult.Data!;

        var result = await _partnerService.CreateAsync(
            request,
            loginUser);

        return Ok(result);
    }

    /// <summary>
    /// 파트너사 수정 API.
    /// </summary>
    [HttpPut("{partnerCode:int}")]
    [ProducesResponseType(typeof(ApiResponse<PartnerSaveResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PartnerSaveResponse>>> UpdatePartner(
        int partnerCode,
        [FromBody] PartnerUpdateRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<PartnerSaveResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var loginUser = loginUserResult.Data!;

        request.PartnerCode = partnerCode;

        var result = await _partnerService.UpdateAsync(
            request,
            loginUser);

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