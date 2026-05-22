namespace poscam.AuthServer.Models.Dtos.Partner;

/// <summary>
/// 파트너사 목록 표시 DTO.
/// </summary>
public class PartnerListItemDto
{
    public int PartnerCode { get; set; }

    public string PartnerName { get; set; } = "";

    public string? PartnerBizNum { get; set; }

    public string? PartnerOwnerName { get; set; }

    public string? PartnerTel { get; set; }

    public string? PartnerEmail { get; set; }

    public int PartnerStatus { get; set; }

    public DateTime RegisteredAt { get; set; }
}