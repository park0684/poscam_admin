using Microsoft.AspNetCore.Mvc;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.Pccam;
using poscam.AuthServer.Services;

namespace poscam.AuthServer.Controllers;

/// <summary>
/// PC캠 인증 API Controller.
/// 
/// PC캠 프로그램에서 호출하는 API를 제공한다.
/// 컨트롤러는 요청/응답만 담당하고,
/// 실제 인증 판단은 PccamAuthService에서 처리한다.
/// </summary>
[ApiController]
[Route("api/pccam")]
public class PccamAuthController : ControllerBase
{
    private readonly PccamAuthService _pccamAuthService;

    public PccamAuthController(PccamAuthService pccamAuthService)
    {
        _pccamAuthService = pccamAuthService;
    }

    /// <summary>
    /// PC캠 최초 인증 API.
    /// 
    /// PC캠 최초 등록 시 사용자가 입력한 인증키와
    /// 현재 장비의 HWID를 기준으로 장비를 인증한다.
    /// 매장코드는 인증키가 연결된 계약정보에서 서버가 자동 확인한다.
    /// </summary>
    /// <param name="request">PC캠 최초 인증 요청 정보</param>
    /// <returns>장비 등록 결과와 인증 토큰</returns>
    [HttpPost("activate")]
    [ProducesResponseType(typeof(ApiResponse<PccamActivateResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PccamActivateResponse>>> Activate(
        [FromBody] PccamActivateRequest request)
    {
        var result = await _pccamAuthService.ActivateAsync(
            request,
            GetClientIp());

        return Ok(result);
    }

    /// <summary>
    /// PC캠 실행 인증 API.
    /// 
    /// 이미 인증된 PC캠이 실행될 때 호출된다.
    /// 서버는 로컬에 저장된 토큰과 HWID를 검증하고,
    /// 현재 라이선스 및 장비 상태가 정상일 경우 새 토큰을 발급한다.
    /// </summary>
    /// <param name="request">PC캠 실행 인증 요청 정보</param>
    /// <returns>인증 유효 여부와 갱신된 인증 토큰</returns>
    [HttpPost("verify")]
    [ProducesResponseType(typeof(ApiResponse<PccamVerifyResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PccamVerifyResponse>>> Verify(
        [FromBody] PccamVerifyRequest request)
    {
        var result = await _pccamAuthService.VerifyAsync(
            request,
            GetClientIp());

        return Ok(result);
    }

    /// <summary>
    /// PC캠 하트비트 API.
    /// 
    /// 현재는 실행 중인 PC캠 장비의 생존 기록을
    /// auth_logs에 남기기 위한 보조 API다.
    /// 인증 정책의 핵심 판단은 verify API가 담당한다.
    /// </summary>
    /// </summary>
    /// <param name="request">PC캠 하트비트 요청 정보</param>
    /// <returns>유효 여부와 서버 시간</returns>
    [HttpPost("heartbeat")]
    [ProducesResponseType(typeof(ApiResponse<PccamHeartbeatResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PccamHeartbeatResponse>>> Heartbeat(
        [FromBody] PccamHeartbeatRequest request)
    {
        var result = await _pccamAuthService.HeartbeatAsync(
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