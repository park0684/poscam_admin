namespace poscam.AuthServer.Models.Dtos.UserManage;

/// <summary>
/// 직원/담당자 상세 조회 DTO.
/// 
/// 담당자 상세 화면에서 사용한다.
/// 비밀번호 해시는 절대 응답에 포함하지 않는다.
/// </summary>
public class UserManageDetailDto
{
    /// <summary>
    /// 사용자 코드.
    /// 화면에 표시하지 않아도 내부 처리에는 필요하다.
    /// </summary>
    public int UserCode { get; set; }

    public int? PartnerCode { get; set; }

    public string? PartnerName { get; set; }

    public string UserId { get; set; } = "";

    public string UserName { get; set; } = "";

    public string? UserCell { get; set; }

    public string? UserEmail { get; set; }

    public int UserRole { get; set; }

    public int UserStatus { get; set; }

    public int? ApprovedBy { get; set; }

    public string? ApprovedByName { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime UserRdate { get; set; }

    public DateTime? UserUdate { get; set; }

    public int? UserRequestType { get; set; }

    public int UserRequestStatus { get; set; }

    public string? UserRequestReason { get; set; }

    public int? UserRequestedBy { get; set; }

    public string? UserRequestedByName { get; set; }

    public DateTime? UserRequestedAt { get; set; }

    public string? UserRequestResultMemo { get; set; }

    /// <summary>
    /// 현재 로그인 사용자가 이 사용자 정보를 수정할 수 있는지 여부.
    /// 
    /// System 또는 필요한 관리 권한을 보유한 관리자 계정은 true.
    /// 파트너 담당자는 직접 수정이 아니라 요청 방식으로 처리한다.
    /// </summary>
    public bool CanEdit { get; set; }

    /// <summary>
    /// 현재 로그인 사용자가 이 사용자에 대한 요청을 등록할 수 있는지 여부.
    /// 담당자는 본인 파트너사 직원에 대해 요청 가능.
    /// </summary>
    public bool CanRequestChange { get; set; }

    /// <summary>
    /// 현재 로그인 사용자가 이 사용자의 상태를 직접 변경할 수 있는지 여부.
    /// 상태 변경은 관리자만 가능.
    /// </summary>
    public bool CanChangeStatus { get; set; }
}