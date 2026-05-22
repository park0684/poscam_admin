namespace poscam.AuthServer.Models.Dtos.Viewer;

/// <summary>
/// 캠뷰어 설정 동기화 요청 DTO.
/// 
/// 캠뷰어에서 로컬 설정을 먼저 저장한 뒤,
/// 서버 접속이 가능하면 서버 DB에 업로드할 때 사용한다.
/// </summary>
public class ViewerConfigSyncRequest
{
    /// <summary>
    /// 매장 코드.
    /// </summary>
    public int StoreCode { get; set; }

    /// <summary>
    /// NVR 설정 정보.
    /// </summary>
    public NvrConfigDto NvrConfig { get; set; } = new();

    /// <summary>
    /// 채널 매핑 정보 목록.
    /// </summary>
    public List<ChannelConfigDto> Channels { get; set; } = new();

    /// <summary>
    /// 설정 버전.
    /// 서버/로컬 설정 비교 기준이다.
    /// </summary>
    public string ConfigVersion { get; set; } = "";

    /// <summary>
    /// 수정자.
    /// 관리자 ID 또는 캠뷰어 사용자명을 저장할 수 있다.
    /// </summary>
    public string? ModifiedBy { get; set; }
}