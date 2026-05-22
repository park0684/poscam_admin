namespace poscam.AuthServer.Models.Dtos.License;

/// <summary>
/// 관리자 화면의 라이선스 발급 요청 DTO.
/// 
/// contractCode는 Route에서 받으므로 Body에는 발급 수량만 받는다.
/// </summary>
public class LicenseIssueManageRequest
{
    /// <summary>
    /// 발급 수량.
    /// </summary>
    public int Count { get; set; }
}