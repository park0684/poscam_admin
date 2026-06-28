namespace poscam.AuthServer.Models.Dtos.Account;

/// <summary>
/// 관리자/담당자 로그인 토큰 내부 Payload DTO.
/// 
/// 관리자 웹에서 API를 호출할 때 사용자를 식별하기 위한 정보다.
/// PC캠/캠뷰어 토큰과 목적이 다르므로 별도 구조로 관리한다.
/// </summary>
public class AccountTokenPayloadDto
{
    /// <summary>
    /// 사용자 코드.
    /// users.user_code 값이다.
    /// </summary>
    public int UserCode { get; set; }

    /// <summary>
    /// 소속 파트너사 코드.
    /// 내부 관리자는 null일 수 있다.
    /// </summary>
    public int? PartnerCode { get; set; }

    /// <summary>
    /// 로그인 ID.
    /// </summary>
    public string UserId { get; set; } = "";

    /// <summary>
    /// 사용자명.
    /// </summary>
    public string UserName { get; set; } = "";

    /// <summary>
    /// 사용자 권한.
    /// 0 = 시스템, 1 = 관리자, 2 = 파트너 담당자.
    /// </summary>
    public int UserRole { get; set; }

    /// <summary>
    /// 사용자 상태.
    /// 0 = 승인대기, 1 = 정상, 2 = 일시중지, 9 = 차단.
    /// </summary>
    public int UserStatus { get; set; }

    /// <summary>
    /// 토큰 발급 시각.
    /// </summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>
    /// 토큰 만료 시각.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
