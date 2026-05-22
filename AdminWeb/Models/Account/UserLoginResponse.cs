namespace poscam.AdminWeb.Models.Account;

/// <summary>
/// 관리자/담당자 로그인 응답 DTO.
/// </summary>
public class UserLoginResponse
{
    public string Token { get; set; } = "";

    public int UserCode { get; set; }

    public int? PartnerCode { get; set; }

    public string UserId { get; set; } = "";

    public string UserName { get; set; } = "";

    public int UserRole { get; set; }

    public int UserStatus { get; set; }

    public DateTime IssuedAt { get; set; }

    public DateTime ExpiresAt { get; set; }
}