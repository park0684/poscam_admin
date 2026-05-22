namespace poscam.AuthServer.Models.Dtos.Admin;

/// <summary>
/// 관리자 계정 기본정보 수정 요청 DTO.
/// 
/// 비밀번호와 권한은 별도 API에서 처리한다.
/// </summary>
public class AdminAccountUpdateRequest
{
    /// <summary>
    /// 수정 대상 관리자 user_code.
    /// </summary>
    public int UserCode { get; set; }

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
    public int UserStatus { get; set; }
}