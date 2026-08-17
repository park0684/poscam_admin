using poscam.AuthServer.Models.Enums;

namespace poscam.AuthServer.Models.Entities;

/// <summary>
/// 매장별 NVR 설정 Entity.
/// DB 테이블: nvr_configs
///
/// 캠뷰어가 매장에 등록된 특정 NVR에 직접 접속하기 위해 필요한 정보를 관리한다.
/// TP-Link VIGI도 클라우드 계정이 아닌 매장 NVR 로컬 접속 정보만 저장한다.
/// NVR 비밀번호는 이미 암호화된 값을 전달받는 기준이므로
/// 서버에서는 추가 암호화하지 않고 그대로 저장한다.
/// </summary>
public class NvrConfig
{
    /// <summary>
    /// 매장 코드.
    /// 다중 NVR 구조에서는 NvrNo와 함께 복합 식별값을 구성한다.
    /// DB 컬럼: nvr_store
    /// </summary>
    public int NvrStore { get; set; }

    /// <summary>
    /// 매장 내부 NVR 번호.
    /// 같은 매장 안에서 유일하며 서버에서 임의 재번호화하지 않는다.
    /// 기존 단일 NVR 데이터는 1을 사용한다.
    /// DB 컬럼: nvr_no
    /// </summary>
    public int NvrNo { get; set; } = 1;

    /// <summary>
    /// 제조사 및 Provider 고정 코드.
    /// DB 컬럼: nvr_provider
    /// </summary>
    public NvrProviderType NvrProvider { get; set; } = NvrProviderType.Dahua;

    /// <summary>
    /// NVR 접속 ID.
    /// DB 컬럼: nvr_id
    /// </summary>
    public string NvrId { get; set; } = "";

    /// <summary>
    /// NVR 접속 비밀번호.
    /// 이미 암호화된 문자열을 저장하는 기준이다.
    /// DB 컬럼: nvr_password
    /// </summary>
    public string NvrPassword { get; set; } = "";

    /// <summary>
    /// NVR IP 또는 도메인 주소.
    /// DB 컬럼: nvr_ip
    /// </summary>
    public string NvrIp { get; set; } = "";

    /// <summary>
    /// 제조사 제어 포트.
    /// Dahua는 NetSDK 포트, TP-Link VIGI는 로컬 OpenAPI HTTPS 포트이다.
    /// DB 컬럼: nvr_port
    /// </summary>
    public int NvrPort { get; set; }

    /// <summary>
    /// 영상 재생용 RTSP 포트.
    /// DB 컬럼: nvr_rtsp_port
    /// </summary>
    public int NvrRtspPort { get; set; } = 554;

    /// <summary>
    /// NVR 전체 채널 수.
    /// 기존 nvr_chenal 오타를 nvr_channels로 수정한 컬럼.
    /// DB 컬럼: nvr_channels
    /// </summary>
    public int? NvrChannels { get; set; }

    /// <summary>
    /// 설정 버전.
    /// 같은 매장의 모든 NVR 행은 하나의 전체 설정 버전을 공유한다.
    /// DB 컬럼: nvr_version
    /// </summary>
    public string NvrVersion { get; set; } = "";

    /// <summary>
    /// NVR 설정 등록일.
    /// DB 컬럼: nvr_rdate
    /// </summary>
    public DateTime? NvrRDate { get; set; }

    /// <summary>
    /// NVR 설정 수정일.
    /// 기존 nvr_edate에서 nvr_udate로 정리한 컬럼.
    /// DB 컬럼: nvr_udate
    /// </summary>
    public DateTime? NvrUDate { get; set; }
}
