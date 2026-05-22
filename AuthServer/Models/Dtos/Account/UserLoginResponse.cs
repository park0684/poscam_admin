namespace poscam.AuthServer.Models.Dtos.Account;

/// <summary>
/// 관리자/담당자 로그인 응답 DTO.
/// 
/// 관리자 프론트엔드는 이 응답의 Token을 저장하고,
/// 이후 API 요청 시 Authorization 헤더에 전달하는 구조로 확장한다.
/// </summary>
public class UserLoginResponse
{
    /// <summary>
    /// 사용자 코드.
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
    /// 1=관리자, 2=담당자
    /// </summary>
    public int UserRole { get; set; }

    /// <summary>
    /// 사용자 상태.
    /// 0=승인대기, 1=정상, 2=일시중지, 9=차단
    /// </summary>
    public int UserStatus { get; set; }

    /// <summary>
    /// 관리자 화면 접근용 토큰.
    /// 추후 별도 AccountTokenService에서 발급한다.
    /// </summary>
    public string Token { get; set; } = "";
}