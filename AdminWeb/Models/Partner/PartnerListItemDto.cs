namespace poscam.AdminWeb.Models.Partner;

/// <summary>
/// 파트너사 목록 화면에서 사용하는 DTO.
/// 
/// 백엔드의 GET /api/admin/partners 응답 Data 항목과 구조를 맞춘다.
/// 목록 화면에서는 상세 주소나 메모까지는 필요 없고,
/// 목록에서 식별 가능한 기본 정보만 표시한다.
/// </summary>
public class PartnerListItemDto
{
    /// <summary>
    /// 파트너사 고유 코드.
    /// partners.partner_code에 해당한다.
    /// </summary>
    public int PartnerCode { get; set; }

    /// <summary>
    /// 파트너사명.
    /// 화면 목록에서 가장 중요한 표시값이다.
    /// </summary>
    public string PartnerName { get; set; } = "";

    /// <summary>
    /// 사업자번호.
    /// 없을 수 있으므로 nullable 처리한다.
    /// </summary>
    public string? PartnerBizNum { get; set; }

    /// <summary>
    /// 대표자명.
    /// </summary>
    public string? PartnerOwnerName { get; set; }

    /// <summary>
    /// 대표 연락처.
    /// </summary>
    public string? PartnerTel { get; set; }

    /// <summary>
    /// 이메일.
    /// </summary>
    public string? PartnerEmail { get; set; }

    /// <summary>
    /// 파트너사 상태.
    /// 1=정상, 0=비활성, 2=일시중지, 9=차단.
    /// </summary>
    public int PartnerStatus { get; set; }

    /// <summary>
    /// 등록일.
    /// 목록에서 등록 순서나 최근 등록 파악에 사용한다.
    /// </summary>
    public DateTime RegisteredAt { get; set; }
}