namespace poscam.AuthServer.Models.Entities;

/// <summary>
/// 매장별 NVR 설정 Entity.
/// DB 테이블: nvr_configs
/// 
/// 캠뷰어가 NVR에 접속하기 위해 필요한 정보를 관리한다.
/// NVR 비밀번호는 이미 암호화된 값을 전달받는 기준이므로
/// 서버에서는 추가 암호화하지 않고 그대로 저장한다.
/// </summary>
public class NvrConfig
{
    /// <summary>
    /// 매장 코드.
    /// 현재 구조에서는 매장 1개당 NVR 설정 1개를 기준으로 한다.
    /// DB 컬럼: nvr_store
    /// </summary>
    public int NvrStore { get; set; }

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
    /// NVR 접속 포트.
    /// DB 컬럼: nvr_port
    /// </summary>
    public int NvrPort { get; set; }

    /// <summary>
    /// NVR 전체 채널 수.
    /// 기존 nvr_chenal 오타를 nvr_channels로 수정한 컬럼.
    /// DB 컬럼: nvr_channels
    /// </summary>
    public int? NvrChannels { get; set; }

    /// <summary>
    /// 설정 버전.
    /// 캠뷰어 로컬 설정과 서버 설정의 동기화 비교에 사용한다.
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