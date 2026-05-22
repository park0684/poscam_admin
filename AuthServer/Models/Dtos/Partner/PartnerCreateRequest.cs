namespace poscam.AuthServer.Models.Dtos.Partner;

/// <summary>
/// 파트너사 등록 요청 DTO.
/// 
/// 파트너사의 역할은 여기서 고정하지 않는다.
/// 역할은 매장 담당자 연결 시 assignment_role로 부여한다.
/// </summary>
public class PartnerCreateRequest
{
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