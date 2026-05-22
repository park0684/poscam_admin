namespace poscam.AuthServer.Models.Dtos.Partner;

/// <summary>
/// 파트너사 수정 요청 DTO.
/// </summary>
public class PartnerUpdateRequest
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

    public int PartnerStatus { get; set; }
}