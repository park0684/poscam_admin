using poscam.AuthServer.Models.Enums;

namespace poscam.AuthServer.Models.Dtos.Config;

/// <summary>
/// 관리자 화면용 NVR 설정 DTO.
///
/// 관리자 화면에서는 NVR 비밀번호를 직접 노출하지 않는다.
/// 저장된 비밀번호가 있는지 여부만 표시한다.
/// </summary>
public class ManageNvrConfigDto
{
    public NvrProviderType NvrProvider { get; set; }

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
    public int NvrRtspPort { get; set; }

    public int? NvrChannels { get; set; }

    public string? NvrVersion { get; set; }
}
