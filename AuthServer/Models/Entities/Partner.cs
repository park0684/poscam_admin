namespace poscam.AuthServer.Models.Entities;

/// <summary>
/// 파트너사 Entity.
/// DB 테이블: partners
/// 
/// 파트너사는 영업사, 설치사, 관리사처럼 고정된 유형으로 보지 않는다.
/// 실제 역할은 매장과 담당자 연결 시 assignment_role로 부여한다.
/// </summary>
public class Partner
{
    /// <summary>
    /// 파트너사 고유 코드.
    /// DB 컬럼: partner_code
    /// </summary>
    public int PartnerCode { get; set; }

    /// <summary>
    /// 파트너사명.
    /// DB 컬럼: partner_name
    /// </summary>
    public string PartnerName { get; set; } = "";

    /// <summary>
    /// 사업자번호.
    /// DB 컬럼: partner_biznum
    /// </summary>
    public string? PartnerBizNum { get; set; }

    /// <summary>
    /// 대표자명.
    /// DB 컬럼: partner_owner_name
    /// </summary>
    public string? PartnerOwnerName { get; set; }

    /// <summary>
    /// 대표 연락처.
    /// DB 컬럼: partner_tel
    /// </summary>
    public string? PartnerTel { get; set; }

    /// <summary>
    /// 대표 이메일.
    /// DB 컬럼: partner_email
    /// </summary>
    public string? PartnerEmail { get; set; }

    /// <summary>
    /// 우편번호.
    /// DB 컬럼: partner_zipcode
    /// </summary>
    public string? PartnerZipcode { get; set; }

    /// <summary>
    /// 기본 주소.
    /// DB 컬럼: partner_address1
    /// </summary>
    public string? PartnerAddress1 { get; set; }

    /// <summary>
    /// 상세 주소.
    /// DB 컬럼: partner_address2
    /// </summary>
    public string? PartnerAddress2 { get; set; }

    /// <summary>
    /// 파트너사 메모.
    /// DB 컬럼: partner_memo
    /// </summary>
    public string? PartnerMemo { get; set; }

    /// <summary>
    /// 파트너사 상태.
    /// PartnerStatus enum 값과 매칭된다.
    /// DB 컬럼: partner_status
    /// </summary>
    public int PartnerStatus { get; set; }

    /// <summary>
    /// 등록일.
    /// DB 컬럼: partner_rdate
    /// </summary>
    public DateTime PartnerRDate { get; set; }

    /// <summary>
    /// 수정일.
    /// DB 컬럼: partner_udate
    /// </summary>
    public DateTime? PartnerUDate { get; set; }
}