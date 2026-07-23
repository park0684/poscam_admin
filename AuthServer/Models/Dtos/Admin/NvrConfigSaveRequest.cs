using poscam.AuthServer.Models.Enums;

namespace poscam.AuthServer.Models.Dtos.Admin;

/// <summary>
/// NVR 설정 저장 요청 DTO.
///
/// 현재 운영 정책상 NVR 설정 수정은 캠뷰어 동기화 API가 기준이다.
/// 기존 관리자 API 호환을 위해 유지하며 동일한 Provider/포트 계약을 사용한다.
/// </summary>
public class NvrConfigSaveRequest
{
    /// <summary>
    /// 설정을 저장할 매장 코드.
    /// </summary>
    public int StoreCode { get; set; }

    /// <summary>
    /// 제조사 및 Provider 고정 코드.
    /// 기존 요청 호환을 위해 기본값은 Dahua이다.
    /// </summary>
    public NvrProviderType NvrProvider { get; set; } = NvrProviderType.Dahua;

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
    /// SDK 또는 로컬 OpenAPI 제어 포트.
    /// </summary>
    public int NvrPort { get; set; }

    /// <summary>
    /// 영상 재생용 RTSP 포트.
    /// </summary>
    public int NvrRtspPort { get; set; } = 554;

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
