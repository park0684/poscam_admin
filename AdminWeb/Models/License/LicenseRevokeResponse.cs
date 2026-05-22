namespace poscam.AdminWeb.Models.License;

/// <summary>
/// 인증키 폐기 응답 DTO.
/// </summary>
public class LicenseRevokeResponse
{
    /// <summary>
    /// 폐기된 라이선스 코드.
    /// </summary>
    public int LicenseCode { get; set; }

    /// <summary>
    /// 라이선스가 속한 계약 코드.
    /// </summary>
    public int ContractCode { get; set; }

    /// <summary>
    /// 계약과 연결된 매장 코드.
    /// 매장 없는 계약은 null.
    /// </summary>
    public int? StoreCode { get; set; }

    /// <summary>
    /// 폐기 성공 여부.
    /// </summary>
    public bool Revoked { get; set; }
}