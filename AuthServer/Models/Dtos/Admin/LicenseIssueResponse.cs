namespace poscam.AuthServer.Models.Dtos.Admin;

/// <summary>
/// PC 캠 라이선스 키 발급 응답 DTO.
/// </summary>
public class LicenseIssueResponse
{
    /// <summary>
    /// 라이선스가 발급된 계약 코드.
    /// </summary>
    public int ContractCode { get; set; }

    /// <summary>
    /// 발급된 라이선스 키 목록.
    /// </summary>
    public List<string> LicenseKeys { get; set; } = new();
}