using Microsoft.AspNetCore.Mvc;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.Config;
using poscam.AuthServer.Models.Dtos.Viewer;
using poscam.AuthServer.Services;

namespace poscam.AuthServer.Controllers;

/// <summary>
/// 캠뷰어 설정 API Controller.
/// 
/// 캠뷰어가 NVR 설정, 채널 매핑 설정, 설정 버전 정보를
/// 서버와 동기화하기 위해 호출하는 API를 제공한다.
/// 
/// 설정 정보에는 NVR 접속 정보가 포함되므로,
/// storeCode만으로 조회하지 않고 토큰 기반으로 권한을 확인한다.
/// </summary>
[ApiController]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    private readonly ConfigService _configService;

    public ConfigController(ConfigService configService)
    {
        _configService = configService;
    }

    /// <summary>
    /// 서버 설정 버전 조회 API.
    /// 
    /// 캠뷰어가 전체 설정을 다운로드하기 전에
    /// 로컬 설정 버전과 서버 설정 버전을 비교하기 위해 호출한다.
    /// </summary>
    /// <param name="request">설정 버전 조회 요청</param>
    /// <returns>서버 설정 버전 및 로컬 최신 여부</returns>
    [HttpPost("version")]
    [ProducesResponseType(typeof(ApiResponse<ConfigVersionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ConfigVersionResponse>>> GetVersion(
        [FromBody] ConfigVersionRequest request)
    {
        var result = await _configService.GetVersionAsync(
            request,
            GetClientIp());

        return Ok(result);
    }

    /// <summary>
    /// 최신 설정 조회 API.
    /// 
    /// 캠뷰어가 실행에 필요한 NVR 접속 설정과
    /// POS 번호별 NVR 채널 매핑 정보를 내려받을 때 호출한다.
    /// </summary>
    /// <param name="request">최신 설정 조회 요청</param>
    /// <returns>NVR 설정 및 채널 매핑 정보</returns>
    [HttpPost("latest")]
    [ProducesResponseType(typeof(ApiResponse<ViewerConfigResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ViewerConfigResponse>>> GetLatestConfig(
        [FromBody] ConfigLatestRequest request)
    {
        var result = await _configService.GetLatestConfigAsync(
            request,
            GetClientIp());

        return Ok(result);
    }

    /// <summary>
    /// 설정 동기화 API.
    /// 
    /// 캠뷰어에서 로컬 설정을 저장한 뒤,
    /// 서버 접속이 가능할 경우 해당 설정을 서버 DB에 업로드한다.
    /// 
    /// 서버는 기존 채널 매핑을 삭제한 뒤 전달받은 채널 목록으로 다시 저장한다.
    /// </summary>
    /// <param name="request">설정 동기화 요청</param>
    /// <returns>동기화 결과</returns>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(ApiResponse<ConfigSyncResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ConfigSyncResponse>>> SyncConfig(
        [FromBody] ConfigSyncRequest request)
    {
        var result = await _configService.SyncConfigAsync(
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