namespace poscam.AuthServer.Models.Dtos.Viewer;

/// <summary>
/// 캠뷰어에 전달할 NVR 설정 DTO.
/// </summary>
public class NvrConfigDto
{
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
    /// NVR 접속 포트.
    /// </summary>
    public int NvrPort { get; set; }

    /// <summary>
    /// NVR 채널 수.
    /// </summary>
    public int? NvrChannels { get; set; }

    /// <summary>
    /// NVR 설정 버전.
    /// </summary>
    public string NvrVersion { get; set; } = "";
}