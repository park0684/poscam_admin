namespace poscam.AuthServer.Models.Dtos.Admin;

/// <summary>
/// NVR 설정 저장 요청 DTO.
/// 
/// 관리자 페이지 또는 캠뷰어 설정 동기화에서 사용할 수 있다.
/// </summary>
public class NvrConfigSaveRequest
{
    /// <summary>
    /// 설정을 저장할 매장 코드.
    /// </summary>
    public int StoreCode { get; set; }

    /// <summary>
    /// NVR 접속 ID.
    /// </summary>
    public string NvrId { get; set; } = "";

    /// <summary>
    /// NVR 접속 비밀번호.
    /// 클라이언트에서 암호화된 값을 전달받아 그대로 저장한다.
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
    /// 설정 버전.
    /// 캠뷰어 로컬 설정과 서버 설정 비교에 사용한다.
    /// </summary>
    public string NvrVersion { get; set; } = "";
}