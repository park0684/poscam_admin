namespace poscam.AdminWeb.Models.Settlement;

/// <summary>
/// 파트너사 단가 정책 조회 DTO.
///
/// AuthServer API:
/// GET /api/manage/settlements/price-policies
/// </summary>
public class PartnerPricePolicyDto
{
    public int PppCode { get; set; }

    public int PartnerCode { get; set; }

    public string? PartnerName { get; set; }

    public int PppPccamPrice { get; set; }

    public int PppViewerPrice { get; set; }

    public int PppStartMonth { get; set; }

    public int? PppEndMonth { get; set; }

    public int PppStatus { get; set; }

    public string? PppMemo { get; set; }

    public DateTime PppRdate { get; set; }

    public DateTime? PppUdate { get; set; }
}