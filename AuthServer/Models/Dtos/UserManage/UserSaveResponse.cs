namespace poscam.AuthServer.Models.Dtos.UserManage;

/// <summary>
/// 직원/담당자 등록/수정 응답 DTO.
/// </summary>
public class UserSaveResponse
{
    /// <summary>
    /// 저장된 사용자 코드.
    /// 화면에는 표시하지 않지만 상세 이동 등에 사용한다.
    /// </summary>
    public int UserCode { get; set; }

    public int? PartnerCode { get; set; }

    public string UserId { get; set; } = "";

    public string UserName { get; set; } = "";

    /// <summary>
    /// 신규 생성 여부.
    /// </summary>
    public bool Created { get; set; }

    /// <summary>
    /// 저장 성공 여부.
    /// </summary>
    public bool Saved { get; set; }
}