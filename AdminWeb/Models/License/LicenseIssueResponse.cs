namespace poscam.AdminWeb.Models.License;

/// <summary>
/// 라이선스 발급 응답 DTO.
/// AuthServer의 LicenseIssueResponse와 구조를 맞춘다.
/// </summary>
public class LicenseIssueResponse
{
    public int ContractCode { get; set; }

    public List<string> LicenseKeys { get; set; } = new();
}