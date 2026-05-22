namespace poscam.AdminWeb.Models.Account;

/// <summary>
/// 관리자/담당자 로그인 요청 DTO.
/// </summary>
public class UserLoginRequest
{
    public string UserId { get; set; } = "";

    public string Password { get; set; } = "";
}