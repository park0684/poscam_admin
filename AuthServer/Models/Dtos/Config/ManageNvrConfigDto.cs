using poscam.AuthServer.Models.Enums;

namespace poscam.AuthServer.Models.Dtos.Config;

/// <summary>
/// 관리자 화면용 NVR 설정 DTO.
///
/// 관리자 화면에서는 NVR 비밀번호를 직접 노출하지 않는다.
/// 저장된 비밀번호가 있는지 여부만 표시한다.
/// 구형 매장 상세 매핑 경로에서는 기존 운영값인 Dahua/RTSP 554를 기본값으로 사용한다.
/// </summary>
public class ManageNvrConfigDto
{
    public NvrProviderType NvrProvider { get; set; } = NvrProviderType.Dahua;

    public string NvrId { get; set; } = "";

    public bool HasPassword { get; set; }

    public string NvrIp { get; set; } = "";

    /// <summary>
    /// SDK 또는 로컬 OpenAPI 제어 포트.
    /// </summary>
    public int NvrPort { get; set; }

    /// <summary>
    /// 영상 재생용 RTSP 포트.
    /// </summary>
    public int NvrRtspPort { get; set; } = 554;

    public int? NvrChannels { get; set; }

    public string? NvrVersion { get; set; }
}
