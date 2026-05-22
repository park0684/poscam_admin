namespace poscam.AuthServer.Models.Dtos.UserManage;

/// <summary>
/// 직원/담당자 신규 등록 요청 DTO.
/// 
/// 관리자:
/// - 모든 파트너사에 담당자 등록 가능
/// 
/// 담당자:
/// - 본인 소속 파트너사에만 담당자 등록 가능
/// 
/// 신규 등록된 담당자는 승인대기 상태로 생성된다.
/// </summary>
public class UserCreateRequest
{
    /// <summary>
    /// 소속 파트너사 코드.
    /// 관리자 등록 시 사용.
    /// 담당자 등록 시에는 서버에서 로그인 사용자의 PartnerCode로 강제한다.
    /// </summary>
    public int? PartnerCode { get; set; }

    /// <summary>
    /// 로그인 ID.
    /// 중복 불가.
    /// </summary>
    public string UserId { get; set; } = "";

    /// <summary>
    /// 초기 비밀번호.
    /// 서버에서 해시 처리 후 users.user_password_hash에 저장한다.
    /// </summary>
    public string Password { get; set; } = "";

    /// <summary>
    /// 담당자 이름.
    /// </summary>
    public string UserName { get; set; } = "";

    /// <summary>
    /// 연락처.
    /// </summary>
    public string? UserCell { get; set; }

    /// <summary>
    /// 이메일.
    /// </summary>
    public string? UserEmail { get; set; }

    /// <summary>
    /// 등록 요청 사유.
    /// 예: 신규 파트너사 직원 등록, 현장 관리 담당자 추가.
    /// </summary>
    public string? RequestReason { get; set; }
}