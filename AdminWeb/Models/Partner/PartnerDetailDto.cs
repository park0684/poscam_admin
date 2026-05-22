namespace poscam.AdminWeb.Models.Partner;

/// <summary>
/// 파트너사 상세 조회 DTO.
/// 
/// 백엔드의 GET /api/admin/partners/{partnerCode} 응답 Data와 구조를 맞춘다.
/// 파트너사 수정 화면에서는 목록보다 많은 정보가 필요하므로
/// 주소, 메모, 수정일 등을 포함한다.
/// </summary>
public class PartnerDetailDto
{
    public int PartnerCode { get; set; }

    public string PartnerName { get; set; } = "";

    public string? PartnerBizNum { get; set; }

    public string? PartnerOwnerName { get; set; }

    public string? PartnerTel { get; set; }

    public string? PartnerEmail { get; set; }

    public string? PartnerZipcode { get; set; }

    public string? PartnerAddress1 { get; set; }

    public string? PartnerAddress2 { get; set; }

    public string? PartnerMemo { get; set; }

    /// <summary>
    /// 파트너사 상태.
    /// 수정 화면에서 관리자만 변경할 수 있다.
    /// </summary>
    public int PartnerStatus { get; set; }

    public DateTime RegisteredAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}