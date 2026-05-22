namespace poscam.AuthServer.Models.Entities;

/// <summary>
/// 등록 장비 Entity.
/// DB 테이블: devices
/// 
/// PC 캠과 캠뷰어 모두 이 테이블에 등록한다.
/// dev_apptype 값으로 PC 캠과 캠뷰어를 구분한다.
/// </summary>
public class Device
{
    /// <summary>
    /// 장비 고유 코드.
    /// DB 컬럼: dev_code
    /// </summary>
    public int DevCode { get; set; }

    /// <summary>
    /// 장비가 연결된 매장 코드.
    /// DB 컬럼: dev_store
    /// </summary>
    public int? DevStore { get; set; }

    /// <summary>
    /// 연결된 라이선스 코드.
    /// PC 캠은 라이선스 코드가 필요하다.
    /// 캠뷰어는 라이선스 키 방식이 아니므로 null 가능.
    /// DB 컬럼: dev_license
    /// </summary>
    public int? DevLicense { get; set; }

    /// <summary>
    /// 앱 유형.
    /// DeviceAppType enum 값과 매칭된다.
    /// 예: 1=PC 캠, 2=캠뷰어
    /// DB 컬럼: dev_apptype
    /// </summary>
    public int? DevAppType { get; set; }

    /// <summary>
    /// 장비 HWID.
    /// PC 캠과 캠뷰어 모두 동일한 기준으로 생성해야 한다.
    /// DB 컬럼: dev_hwid
    /// </summary>
    public string DevHwid { get; set; } = "";

    /// <summary>
    /// POS 번호.
    /// PC 캠은 실제 POS 번호를 사용한다.
    /// 캠뷰어는 POS 번호가 없으므로 0으로 저장하는 기준을 권장한다.
    /// DB 컬럼: dev_pos
    /// </summary>
    public int DevPos { get; set; }

    /// <summary>
    /// 장비명.
    /// 예: POS1-PC, 사무실 캠뷰어, 점주 노트북 등
    /// DB 컬럼: dev_name
    /// </summary>
    public string? DevName { get; set; }

    /// <summary>
    /// 장비 등록일.
    /// DB 컬럼: dev_rdate
    /// </summary>
    public DateTime? DevRDate { get; set; }
}