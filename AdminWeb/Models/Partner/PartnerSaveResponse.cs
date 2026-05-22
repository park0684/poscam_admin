namespace poscam.AdminWeb.Models.Partner;

/// <summary>
/// 파트너사 등록/수정 응답 DTO.
/// 
/// 백엔드의 등록/수정 응답 Data와 맞춘다.
/// 등록 후 상세 페이지로 이동하기 위해 PartnerCode가 필요하다.
/// </summary>
public class PartnerSaveResponse
{
    public int PartnerCode { get; set; }

    public string PartnerName { get; set; } = "";

    public bool Saved { get; set; }
}