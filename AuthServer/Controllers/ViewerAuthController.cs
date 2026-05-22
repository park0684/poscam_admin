using Microsoft.AspNetCore.Mvc;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.Viewer;
using poscam.AuthServer.Services;

namespace poscam.AuthServer.Controllers;

/// <summary>
/// 캠뷰어 인증 API Controller.
/// 
/// 캠뷰어 프로그램에서 호출하는 API를 제공한다.
/// 
/// 캠뷰어는 최초 실행 또는 토큰이 없는 경우 매장 ID/비밀번호로 로그인하고,
/// 이후 실행부터는 저장된 토큰으로 실행 인증을 수행한다.
/// </summary>
[ApiController]
[Route("api/viewer")]
public class ViewerAuthController : ControllerBase
{
    private readonly ViewerAuthService _viewerAuthService;

    public ViewerAuthController(ViewerAuthService viewerAuthService)
    {
        _viewerAuthService = viewerAuthService;
    }

    /// <summary>
    /// 캠뷰어 최초 로그인 API.
    /// 
    /// 이 API는 프로그램 사용 자체가 목적이 아니라,
    /// 캠뷰어 실행에 필요한 인증 토큰을 발급받기 위한 API다.
    /// 
    /// 최초 실행 또는 로컬 토큰이 없는 경우 호출한다.
    /// </summary>
    /// <param name="request">캠뷰어 로그인 요청 정보</param>
    /// <returns>캠뷰어 장비 코드, 설정 버전, 인증 토큰</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<ViewerLoginResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ViewerLoginResponse>>> Login(
        [FromBody] ViewerLoginRequest request)
    {
        var result = await _viewerAuthService.LoginAsync(
            request,
            GetClientIp());

        return Ok(result);
    }

    /// <summary>
    /// 캠뷰어 토큰 실행 인증 API.
    /// 
    /// 최초 로그인 이후에는 매장 ID/비밀번호를 다시 입력하지 않고,
    /// 로컬에 저장된 토큰으로 프로그램 실행 가능 여부를 확인한다.
    /// 
    /// 토큰이 유효하더라도 devices 테이블에 해당 장비가 없으면
    /// 사용 해제된 장비로 보고 실행을 차단한다.
    /// </summary>
    /// <param name="request">캠뷰어 토큰 인증 요청 정보</param>
    /// <returns>실행 가능 여부와 갱신된 인증 토큰</returns>
    [HttpPost("verify-token")]
    [ProducesResponseType(typeof(ApiResponse<ViewerTokenVerifyResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ViewerTokenVerifyResponse>>> VerifyToken(
        [FromBody] ViewerTokenVerifyRequest request)
    {
        var result = await _viewerAuthService.VerifyTokenAsync(
            request,
            GetClientIp());

        return Ok(result);
    }

    /// <summary>
    /// 캠뷰어 등록 장비 목록 조회 API.
    /// 
    /// storeCode만으로 장비 목록을 조회하지 않는다.
    /// 매장 ID/비밀번호를 검증한 뒤 해당 매장의 캠뷰어 장비 목록을 반환한다.
    /// 
    /// 주 사용처:
    /// - 캠뷰어 슬롯 초과 시 기존 장비 해제 화면
    /// </summary>
    [HttpPost("devices")]
    [ProducesResponseType(typeof(ApiResponse<List<DeviceSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<DeviceSummaryDto>>>> GetViewerDevices(
        [FromBody] ViewerDeviceListRequest request)
    {
        var result = await _viewerAuthService.GetViewerDevicesWithLoginAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// 캠뷰어 장비 해제 API.
    /// 
    /// 캠뷰어는 PC캠과 달리 사용자가 매장 ID/비밀번호를 입력한 뒤
    /// 기존 등록 장비를 직접 해제할 수 있다.
    /// 
    /// 장비가 해제되면 devices 테이블에서 삭제된다.
    /// 따라서 해당 장비에 기존 토큰이 남아 있어도
    /// 이후 온라인 토큰 인증 시 실행이 차단된다.
    /// </summary>
    /// <param name="request">캠뷰어 장비 해제 요청 정보</param>
    /// <returns>장비 해제 결과</returns>
    [HttpDelete("devices/release")]
    [ProducesResponseType(typeof(ApiResponse<ViewerDeviceReleaseResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ViewerDeviceReleaseResponse>>> ReleaseViewerDevice(
        [FromBody] ViewerDeviceReleaseRequest request)
    {
        var result = await _viewerAuthService.ReleaseViewerDeviceAsync(
            request,
            GetClientIp());

        return Ok(result);
    }

    /// <summary>
    /// 요청 클라이언트 IP를 가져온다.
    /// 
    /// 프록시나 로드밸런서 뒤에 서버가 있을 경우
    /// X-Forwarded-For 헤더를 우선 확인한다.
    /// </summary>
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