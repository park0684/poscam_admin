using poscam.AuthServer.Models.Enums;

namespace poscam.AuthServer.Models.Dtos.Admin;

/// <summary>
/// 관리자 계정 신규 등록 요청 DTO.
/// 
/// 관리자 계정은 파트너사에 소속되지 않으며,
/// 생성 시 UserRole은 서버에서 Admin으로 고정한다.
/// </summary>
public class AdminAccountCreateRequest
{
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
    /// 관리자 이름.
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
    /// 사용자 상태.
    /// 기본값은 정상 상태.
    /// </summary>
    public int UserStatus { get; set; } = 1;

    /// <summary>
    /// 생성 시 함께 부여할 관리자 권한 코드 목록.
    /// DB에는 숫자 코드만 저장한다.
    /// </summary>
    public List<int> PermissionCodes { get; set; } = new();
}