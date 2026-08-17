using poscam.AuthServer.Models.Enums;

namespace poscam.AuthServer.Models.Dtos.Viewer;

/// <summary>
/// 캠뷰어에 전달하거나 캠뷰어에서 업로드하는 NVR 설정 DTO.
/// Provider 코드는 CamViewer의 NvrProviderType 정수값과 동일해야 한다.
/// </summary>
public class NvrConfigDto
{
    /// <summary>
    /// 매장 내부 NVR 번호.
    /// ConfigSchemaVersion 2 이상에서는 필수이며 1 이상의 값을 사용한다.
    /// 구버전 요청에서 누락된 경우 0으로 바인딩되고 서비스 계층에서 NVR 1로 정규화한다.
    /// </summary>
    public int NvrNo { get; set; }

    /// <summary>
    /// 제조사 및 Provider 고정 코드.
    /// 0=미지정, 1=Dahua, 2=TP-Link VIGI, 3=KT Telecop.
    /// </summary>
    public NvrProviderType NvrProvider { get; set; }

    /// <summary>
    /// NVR 접속 ID.
    /// </summary>
    public string NvrId { get; set; } = "";

    /// <summary>
    /// NVR 접속 비밀번호.
    /// 서버에서는 이미 암호화된 문자열로 보고 그대로 전달한다.
    /// </summary>
    public string NvrPassword { get; set; } = "";

    /// <summary>
    /// NVR IP 또는 도메인.
    /// </summary>
    public string NvrIp { get; set; } = "";

    /// <summary>
    /// SDK 또는 로컬 OpenAPI 제어 포트.
    /// 기존 JSON 필드명 호환을 위해 NvrPort를 유지한다.
    /// </summary>
    public int NvrPort { get; set; }

    /// <summary>
    /// 영상 재생용 RTSP 포트.
    /// </summary>
    public int NvrRtspPort { get; set; }

    /// <summary>
    /// NVR 채널 수.
    /// </summary>
    public int? NvrChannels { get; set; }

    /// <summary>
    /// NVR 설정 버전.
    /// 같은 매장의 모든 NVR은 하나의 ConfigVersion을 공유한다.
    /// </summary>
    public string NvrVersion { get; set; } = "";
}
