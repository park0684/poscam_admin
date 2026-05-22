namespace poscam.AuthServer.Models.Dtos.Account;

/// <summary>
/// 담당자 회원가입 응답 DTO.
/// </summary>
public class UserRegisterResponse
{
    /// <summary>
    /// 생성된 사용자 코드.
    /// </summary>
    public int UserCode { get; set; }

    /// <summary>
    /// 로그인 ID.
    /// </summary>
    public string UserId { get; set; } = "";

    /// <summary>
    /// 사용자명.
    /// </summary>
    public string UserName { get; set; } = "";

    /// <summary>
    /// 승인 대기 여부.
    /// </summary>
    public bool IsPendingApproval { get; set; }
}