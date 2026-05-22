namespace poscam.AuthServer.Models.Dtos.Account;

/// <summary>
/// 담당자 회원가입 요청 DTO.
/// 
/// 담당자는 가입 후 관리자 승인을 받아야 한다.
/// 일반 담당자는 스스로 관리자 권한을 선택할 수 없다.
/// </summary>
public class UserRegisterRequest
{
    /// <summary>
    /// 소속 파트너사 코드.
    /// 가입 시 파트너사를 선택하거나, 추후 관리자가 지정할 수 있다.
    /// </summary>
    public int? PartnerCode { get; set; }

    /// <summary>
    /// 로그인 ID.
    /// </summary>
    public string UserId { get; set; } = "";

    /// <summary>
    /// 로그인 비밀번호.
    /// 서버에서는 해시 처리 후 저장한다.
    /// </summary>
    public string Password { get; set; } = "";

    /// <summary>
    /// 담당자명.
    /// </summary>
    public string UserName { get; set; } = "";

    /// <summary>
    /// 담당자 연락처.
    /// </summary>
    public string? UserCell { get; set; }

    /// <summary>
    /// 담당자 이메일.
    /// </summary>
    public string? UserEmail { get; set; }
}