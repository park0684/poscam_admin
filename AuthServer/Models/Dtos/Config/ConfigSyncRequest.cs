using poscam.AuthServer.Models.Dtos.Viewer;

namespace poscam.AuthServer.Models.Dtos.Config;

/// <summary>
/// 캠뷰어 설정 동기화 요청 DTO.
///
/// 캠뷰어에서 로컬 설정을 저장한 뒤,
/// 서버 접속이 가능하면 이 API로 서버 DB에 업로드한다.
/// </summary>
public class ConfigSyncRequest
{
    /// <summary>
    /// 캠뷰어 인증 토큰.
    /// </summary>
    public string Token { get; set; } = "";

    /// <summary>
    /// 현재 캠뷰어 장비 HWID.
    /// </summary>
    public string Hwid { get; set; } = "";

    /// <summary>
    /// 요청 설정 스키마 버전.
    /// 값이 없거나 2 미만이면 기존 단일 NVR 요청으로 처리한다.
    /// </summary>
    public int ConfigSchemaVersion { get; set; }

    /// <summary>
    /// 다중 NVR 설정 목록.
    /// ConfigSchemaVersion 2 이상에서 사용한다.
    /// </summary>
    public List<NvrConfigDto> Nvrs { get; set; } = new();

    /// <summary>
    /// 구버전 단일 NVR CamViewer 호환용 설정.
    /// Schema 2 CamViewer는 Nvrs를 사용한다.
    /// </summary>
    public NvrConfigDto? NvrConfig { get; set; }

    /// <summary>
    /// POS 번호와 NVR 채널 매핑 목록.
    /// Schema 2에서는 각 항목의 NvrNo가 필수이다.
    /// </summary>
    public List<ChannelConfigDto> Channels { get; set; } = new();

    /// <summary>
    /// 설정 버전.
    /// 비어 있으면 서버에서 자동 생성한다.
    /// </summary>
    public string? ConfigVersion { get; set; }

    /// <summary>
    /// 수정자.
    /// </summary>
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// 프로그램 버전.
    /// </summary>
    public string? ProgramVersion { get; set; }
}
