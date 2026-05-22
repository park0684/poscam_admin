namespace poscam.AuthServer.Models.Dtos.Partner;

/// <summary>
/// 파트너사 등록/수정 응답 DTO.
/// </summary>
public class PartnerSaveResponse
{
    public int PartnerCode { get; set; }

    public string PartnerName { get; set; } = "";

    public bool Saved { get; set; }
}