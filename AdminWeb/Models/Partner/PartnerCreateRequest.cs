namespace poscam.AdminWeb.Models.Partner;

/// <summary>
/// 신규 파트너사 등록 요청 DTO.
/// 
/// POST /api/admin/partners 호출 시 사용한다.
/// 신규 등록 시에는 partnerCode가 아직 없으므로 포함하지 않는다.
/// </summary>
public class PartnerCreateRequest
{
    /// <summary>
    /// 파트너사명.
    /// 필수 입력값으로 화면에서 검증한다.
    /// </summary>
    public string PartnerName { get; set; } = "";

    public string? PartnerBizNum { get; set; }

    public string? PartnerOwnerName { get; set; }

    public string? PartnerTel { get; set; }

    public string? PartnerEmail { get; set; }

    public string? PartnerZipcode { get; set; }

    public string? PartnerAddress1 { get; set; }

    public string? PartnerAddress2 { get; set; }

    public string? PartnerMemo { get; set; }
}