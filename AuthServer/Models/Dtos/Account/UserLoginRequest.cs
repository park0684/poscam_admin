namespace poscam.AuthServer.Models.Dtos.Account;

/// <summary>
/// 관리자/담당자 로그인 요청 DTO.
/// </summary>
public class UserLoginRequest
{
    /// <summary>
    /// 로그인 ID.
    /// </summary>
    public string UserId { get; set; } = "";

    /// <summary>
    /// 로그인 비밀번호.
    /// </summary>
    public string Password { get; set; } = "";
}