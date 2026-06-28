namespace poscam.AuthServer.Models.Dtos.Account;

/// <summary>
/// 현재 로그인 사용자의 역할과 관리자 세부 권한 정보.
/// </summary>
public class CurrentUserAccessResponse
{
    public int UserCode { get; set; }

    public string UserName { get; set; } = "";

    public int UserRole { get; set; }

    public List<int> PermissionCodes { get; set; } = new();
}
