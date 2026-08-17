using Microsoft.AspNetCore.Mvc;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.Config;
using poscam.AuthServer.Models.Dtos.Viewer;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Services;
using System.Threading;

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
    private const int MultiNvrConfigSchemaVersion = 2;

    /*
     * Schema 1 업로드 보호는 "현재 서버 설정 확인 → 실제 Sync" 두 단계다.
     * 두 단계 사이에 다른 Schema 2 Sync가 끼면 legacy 요청이 새 다중 NVR 설정을
     * 다시 NVR 1 한 대로 덮어쓸 수 있으므로 현재 단일 AuthServer 프로세스에서는
     * 모든 Config Sync를 직렬화한다.
     *
     * 설정 저장은 사용자 조작 시에만 발생하는 저빈도 작업이므로 전역 직렬화 비용은 작다.
     * AuthServer를 여러 인스턴스로 확장할 경우에는 DB advisory lock 또는
     * 설정 revision 기반 optimistic concurrency로 교체해야 한다.
     */
    private static readonly SemaphoreSlim ConfigSyncGate =
        new SemaphoreSlim(1, 1);

    private readonly ConfigService _configService;

    public ConfigController(ConfigService configService)
    {
        _configService = configService;
    }

    /// <summary>
    /// 설정 API 스키마 기능 조회.
    ///
    /// 매장/NVR/인증 정보를 반환하지 않는 정적 capability endpoint다.
    /// 신규 CamViewer는 다중 NVR 설정을 업로드하기 전에 이 endpoint가 존재하고
    /// Schema 2를 지원하는지 확인하여 구형 AuthServer에 설정이 축약 저장되는 것을 막는다.
    /// </summary>
    [HttpPost("capabilities")]
    [ProducesResponseType(typeof(ApiResponse<ConfigCapabilitiesResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<ConfigCapabilitiesResponse>> GetCapabilities()
    {
        return Ok(
            ApiResponse<ConfigCapabilitiesResponse>.Ok(
                new ConfigCapabilitiesResponse(),
                "설정 API 지원 기능을 조회했습니다."));
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
    ///
    /// Schema 1 CamViewer는 정확히 NVR 1 한 대와 NVR 1 소속 채널만
    /// 손실 없이 표현할 수 있다. NVR 2 한 대만 남은 구성도 Schema 2가 필요하다.
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

        if (request.ConfigSchemaVersion < MultiNvrConfigSchemaVersion &&
            result.Success &&
            !LegacyConfigSyncPolicy.IsLegacyRepresentable(result.Data))
        {
            return Ok(
                ApiResponse<ViewerConfigResponse>.Fail(
                    AuthErrorCode.ConfigSchemaNotSupported,
                    "이 매장의 NVR 번호/채널 설정은 구버전 CamViewer로 안전하게 표현할 수 없습니다. 다중 NVR 설정 스키마를 지원하는 최신 CamViewer가 필요합니다."));
        }

        return Ok(result);
    }

    /// <summary>
    /// 설정 동기화 API.
    /// 
    /// 캠뷰어에서 로컬 설정을 저장한 뒤,
    /// 서버 접속이 가능할 경우 해당 설정을 서버 DB에 업로드한다.
    ///
    /// Schema 2 미만의 구버전 CamViewer는 NVR 번호를 보존할 수 없다.
    /// 따라서 기존 서버 설정이 legacy로 손실 없이 표현 가능한 경우에만 쓰기를 허용한다.
    /// </summary>
    /// <param name="request">설정 동기화 요청</param>
    /// <returns>동기화 결과</returns>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(ApiResponse<ConfigSyncResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ConfigSyncResponse>>> SyncConfig(
        [FromBody] ConfigSyncRequest request)
    {
        await ConfigSyncGate.WaitAsync();

        try
        {
            var clientIp = GetClientIp();

            if (request.ConfigSchemaVersion < MultiNvrConfigSchemaVersion)
            {
                /*
                 * 기존 NVR 설정이 아직 없는 신규 매장은 NvrConfigNotFound가 반환되므로
                 * legacy 최초 단일 NVR 업로드를 계속 허용한다.
                 *
                 * 기존 설정이 있으면 다음 조건을 모두 만족해야 legacy write를 허용한다.
                 * - NVR 정확히 1대
                 * - NvrNo = 1
                 * - 모든 채널이 NVR 1 참조
                 *
                 * 따라서 NVR 2 한 대만 남은 구성도 legacy write를 차단한다.
                 *
                 * ConfigSyncGate를 잡은 상태에서 검사와 Sync를 연속 실행하므로
                 * 같은 프로세스의 다른 Config Sync가 두 단계 사이에 끼어들 수 없다.
                 */
                var existingConfigCheck = await _configService.GetLatestConfigAsync(
                    new ConfigLatestRequest
                    {
                        Token = request.Token,
                        Hwid = request.Hwid,
                        ConfigSchemaVersion = request.ConfigSchemaVersion,
                        LocalConfigVersion = request.ConfigVersion,
                        ProgramVersion = request.ProgramVersion
                    },
                    clientIp);

                if (!LegacyConfigSyncPolicy.CanContinue(existingConfigCheck))
                {
                    if (existingConfigCheck.Success)
                    {
                        return Ok(
                            ApiResponse<ConfigSyncResponse>.Fail(
                                AuthErrorCode.ConfigSchemaNotSupported,
                                "현재 서버 설정의 NVR 번호/채널 매핑은 구버전 CamViewer로 안전하게 저장할 수 없습니다. 최신 CamViewer가 필요합니다."));
                    }

                    return Ok(
                        ApiResponse<ConfigSyncResponse>.Fail(
                            existingConfigCheck.ErrorCode,
                            existingConfigCheck.Message));
                }
            }

            var result = await _configService.SyncConfigAsync(
                request,
                clientIp);

            return Ok(result);
        }
        finally
        {
            ConfigSyncGate.Release();
        }
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
