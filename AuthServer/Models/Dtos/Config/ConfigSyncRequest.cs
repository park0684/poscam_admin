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
    /// NVR 설정 정보.
    /// </summary>
    public NvrConfigDto NvrConfig { get; set; } = new();

    /// <summary>
    /// POS 번호와 NVR 채널 매핑 목록.
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