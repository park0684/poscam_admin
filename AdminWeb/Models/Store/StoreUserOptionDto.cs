namespace poscam.AdminWeb.Models.Store;

/// <summary>
/// 매장 등록/수정 화면에서 담당자 선택용으로 사용하는 DTO.
/// 
/// /api/manage/users 응답 중 필요한 필드만 받는다.
/// 서버 응답에 추가 필드가 있어도 필요한 속성만 매핑된다.
/// </summary>
public class StoreUserOptionDto
{
    public int UserCode { get; set; }

    public string UserName { get; set; } = "";

    public string? UserCell { get; set; }

    public int? PartnerCode { get; set; }

    public string? PartnerName { get; set; }

    public int UserStatus { get; set; }
}