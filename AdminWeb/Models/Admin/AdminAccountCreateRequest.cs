namespace poscam.AdminWeb.Models.Admin;

/// <summary>
/// 관리자 계정 신규 등록 요청 DTO.
/// 
/// AuthServer의 api/admin/accounts POST 요청에 사용한다.
/// </summary>
public class AdminAccountCreateRequest
{
    /// <summary>
    /// 로그인 ID.
    /// </summary>
    public string UserId { get; set; } = "";

    /// <summary>
    /// 초기 비밀번호.
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
    /// 1=정상, 2=일시중지, 3=무효, 9=차단.
    /// </summary>
    public int UserStatus { get; set; } = 1;

    /// <summary>
    /// 생성 시 함께 부여할 관리자 권한 코드 목록.
    /// </summary>
    public List<int> PermissionCodes { get; set; } = new();
}