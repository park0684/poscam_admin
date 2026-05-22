namespace poscam.AuthServer.Models.Entities;

/// <summary>
/// PC 캠 라이선스 키 Entity.
/// DB 테이블: licensekeys
/// 
/// PC 캠은 인증키 1개가 PC 1대에 귀속된다.
/// 캠뷰어는 기본적으로 매장 계정 + 슬롯 방식이므로,
/// 이 테이블은 우선 PC 캠 인증키 중심으로 사용한다.
/// </summary>
public class LicenseKey
{
    /// <summary>
    /// 라이선스 고유 코드.
    /// DB 컬럼: lic_code
    /// </summary>
    public int LicCode { get; set; }

    /// <summary>
    /// 연결된 계약 코드.
    /// DB 컬럼: lic_contract
    /// </summary>
    public int LicContract { get; set; }

    /// <summary>
    /// 실제 인증키 문자열.
    /// 예: PCM-A8K2-7M9P-W3X4
    /// DB 컬럼: lic_key
    /// </summary>
    public string LicKey { get; set; } = "";

    /// <summary>
    /// 라이선스 상태.
    /// LicenseStatus enum 값과 매칭된다.
    /// 예: 0=미사용, 1=사용중, 2=초기화, 9=폐기
    /// DB 컬럼: lic_status
    /// </summary>
    public int LicStatus { get; set; }

    /// <summary>
    /// 라이선스 발급일.
    /// DB 컬럼: lic_rdate
    /// </summary>
    public DateTime LicRDate { get; set; }
}