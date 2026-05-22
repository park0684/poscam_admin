namespace poscam.AuthServer.Models.Dtos.Viewer;

/// <summary>
/// 캠뷰어 설정 조회 응답 DTO.
/// 
/// 캠뷰어가 실행될 때 NVR 접속 정보와 채널 매핑 정보를 받기 위해 사용한다.
/// </summary>
public class ViewerConfigResponse
{
    /// <summary>
    /// 매장 코드.
    /// </summary>
    public int StoreCode { get; set; }

    /// <summary>
    /// 서버 설정 버전.
    /// </summary>
    public string ConfigVersion { get; set; } = "";

    /// <summary>
    /// NVR 접속 설정.
    /// </summary>
    public NvrConfigDto NvrConfig { get; set; } = new();

    /// <summary>
    /// POS 번호와 NVR 채널 매핑 목록.
    /// </summary>
    public List<ChannelConfigDto> Channels { get; set; } = new();
}