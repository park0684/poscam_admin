namespace poscam.AdminWeb.Models.Partner;

/// <summary>
/// 파트너사 수정 요청 DTO.
/// 
/// PUT /api/admin/partners/{partnerCode} 호출 시 사용한다.
/// 수정 시에는 대상 파트너사 코드와 상태값이 필요하다.
/// </summary>
public class PartnerUpdateRequest
{
    /// <summary>
    /// 수정 대상 파트너사 코드.
    /// Route에도 partnerCode가 들어가지만, Body에도 함께 넣어 백엔드 DTO와 맞춘다.
    /// </summary>
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
    /// 상태값.
    /// 1=정상, 0=비활성, 2=일시중지, 9=차단.
    /// 신규 등록 화면에서는 기본값 1을 사용한다.
    /// </summary>
    public int PartnerStatus { get; set; } = 1;
}