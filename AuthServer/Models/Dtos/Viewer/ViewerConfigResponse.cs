namespace poscam.AuthServer.Models.Dtos.Viewer;

/// <summary>
/// 캠뷰어 설정 조회 응답 DTO.
///
/// 캠뷰어가 실행될 때 NVR 접속 정보와 채널 매핑 정보를 받기 위해 사용한다.
/// ConfigSchemaVersion 2부터 다중 NVR 목록과 채널별 NvrNo를 사용한다.
/// </summary>
public class ViewerConfigResponse
{
    /// <summary>
    /// 현재 응답 설정 스키마 버전.
    /// 1=기존 단일 NVR, 2=다중 NVR.
    /// </summary>
    public int ConfigSchemaVersion { get; set; } = 2;

    /// <summary>
    /// 매장 코드.
    /// </summary>
    public int StoreCode { get; set; }

    /// <summary>
    /// 서버 설정 버전.
    /// </summary>
    public string ConfigVersion { get; set; } = "";

    /// <summary>
    /// 다중 NVR 접속 설정 목록.
    /// Schema 2 CamViewer는 이 목록을 기준으로 사용한다.
    /// </summary>
    public List<NvrConfigDto> Nvrs { get; set; } = new();

    /// <summary>
    /// 구버전 단일 NVR CamViewer 호환용 필드.
    /// 단일 NVR 매장에서만 호환 목적으로 사용하며 신규 CamViewer는 Nvrs를 사용한다.
    /// </summary>
    public NvrConfigDto? NvrConfig { get; set; }

    /// <summary>
    /// POS 번호와 NVR 채널 매핑 목록.
    /// Schema 2에서는 각 항목의 NvrNo가 필수이다.
    /// </summary>
    public List<ChannelConfigDto> Channels { get; set; } = new();
}
