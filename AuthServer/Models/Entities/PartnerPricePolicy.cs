namespace poscam.AuthServer.Models.Entities;

/// <summary>
/// 파트너사별 단가 정책 Entity.
///
/// DB 테이블: partner_price_policy
///
/// 역할:
/// - 파트너사별 PC캠 월 단가를 관리한다.
/// - 파트너사별 캠뷰어 월 단가를 관리한다.
/// - 적용 시작월/종료월을 통해 단가 이력을 보존한다.
/// - 월별 청구자료 생성 시 해당 월에 유효한 단가를 조회하는 기준이 된다.
/// </summary>
public class PartnerPricePolicy
{
    /// <summary>
    /// 단가 정책 코드.
    /// DB 컬럼: ppp_code
    /// </summary>
    public int PppCode { get; set; }

    /// <summary>
    /// 파트너사 코드.
    /// DB 컬럼: partner_code
    /// </summary>
    public int PartnerCode { get; set; }

    /// <summary>
    /// PC캠 월 단가.
    /// DB 컬럼: ppp_pccam_price
    /// </summary>
    public int PppPccamPrice { get; set; }

    /// <summary>
    /// 캠뷰어 월 단가.
    /// DB 컬럼: ppp_viewer_price
    /// </summary>
    public int PppViewerPrice { get; set; }

    /// <summary>
    /// 적용 시작월.
    /// 예: 202605
    /// DB 컬럼: ppp_start_month
    /// </summary>
    public int PppStartMonth { get; set; }

    /// <summary>
    /// 적용 종료월.
    /// NULL이면 종료 없음.
    /// DB 컬럼: ppp_end_month
    /// </summary>
    public int? PppEndMonth { get; set; }

    /// <summary>
    /// 단가 정책 상태.
    /// 1=사용, 0=미사용.
    /// DB 컬럼: ppp_status
    /// </summary>
    public int PppStatus { get; set; }

    /// <summary>
    /// 메모.
    /// DB 컬럼: ppp_memo
    /// </summary>
    public string? PppMemo { get; set; }

    /// <summary>
    /// 등록일.
    /// DB 컬럼: ppp_rdate
    /// </summary>
    public DateTime PppRdate { get; set; }

    /// <summary>
    /// 수정일.
    /// DB 컬럼: ppp_udate
    /// </summary>
    public DateTime? PppUdate { get; set; }
}

