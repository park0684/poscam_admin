namespace poscam.AuthServer.Models.Dtos.Admin;

/// <summary>
/// PC 캠 라이선스 키 발급 요청 DTO.
/// 
/// 특정 계약에 대해 PC 캠 인증키를 여러 개 발급할 때 사용한다.
/// </summary>
public class LicenseIssueRequest
{
    /// <summary>
    /// 라이선스를 발급할 계약 코드.
    /// </summary>
    public int ContractCode { get; set; }

    /// <summary>
    /// 발급할 인증키 수량.
    /// 계약의 PC 캠 허용 수량을 초과하지 않아야 한다.
    /// </summary>
    public int Count { get; set; }
}