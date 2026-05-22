namespace poscam.AuthServer.Models.Entities;

/// <summary>
/// 계약 정보를 나타내는 Entity.
/// DB 테이블: contracts
/// 
/// 매장별 PC 캠 허용 수량, 캠뷰어 허용 수량,
/// 계약 유형, 계약 기간, 계약 상태를 관리한다.
/// </summary>
public class Contract
{
    /// <summary>
    /// 계약 고유 코드.
    /// DB 컬럼: con_code
    /// </summary>
    public int ConCode { get; set; }

    /// <summary>
    /// 계약이 연결된 매장 코드.
    /// 매장과 계약이 연결되지 않을 수도 있으므로 null 허용으로 설정.
    /// DB 컬럼: con_store
    /// </summary>
    public int? ConStore { get; set; }

    /// <summary>
    /// 계약 번호.
    /// 외부 계약서 번호 또는 내부 관리번호로 사용할 수 있다.
    /// DB 컬럼: con_no
    /// </summary>
    public string ConNo { get; set; } = "";

    /// <summary>
    /// 계약 유형.
    /// ContractType enum 값과 매칭된다.
    /// 예: 1=테스트, 2=구매형, 3=구독형
    /// DB 컬럼: con_type
    /// </summary>
    public int ConType { get; set; }

    /// <summary>
    /// PC 캠 허용 수량.
    /// PC 캠 인증키 발급 수량 및 장비 등록 제한에 사용된다.
    /// DB 컬럼: con_pcc
    /// </summary>
    public int ConPcc { get; set; }

    /// <summary>
    /// 캠뷰어 허용 수량.
    /// 캠뷰어 슬롯 수량 검증에 사용된다.
    /// DB 컬럼: con_view
    /// </summary>
    public int ConView { get; set; }

    /// <summary>
    /// 계약 시작일.
    /// DB 컬럼: con_start
    /// </summary>
    public DateTime ConStart { get; set; }

    /// <summary>
    /// 계약 종료일.
    /// 구매형 계약은 null 허용 가능.
    /// 테스트형/구독형은 종료일 기준 검증이 필요하다.
    /// DB 컬럼: con_end
    /// </summary>
    public DateTime? ConEnd { get; set; }

    /// <summary>
    /// 계약 상태.
    /// ContractStatus enum 값과 매칭된다.
    /// 예: 0=비활성, 1=정상, 2=만료, 3=일시중지, 9=해지
    /// DB 컬럼: status 또는 con_status
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// 계약 등록일.
    /// DB 컬럼: con_rdate
    /// </summary>
    public DateTime ConRDate { get; set; }

    /// <summary>
    /// 계약 수정일.
    /// DB 컬럼: con_udate
    /// </summary>
    public DateTime? ConUDate { get; set; }

    /// <summary>
    /// 계약 파트너사 
    /// </summary>
    public int ConPartner { get; set; }
}