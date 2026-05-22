namespace poscam.AdminWeb.Models.License;

/// <summary>
/// 인증키 폐기 요청 DTO.
/// </summary>
public class LicenseRevokeManageRequest
{
    /// <summary>
    /// 폐기 사유.
    /// </summary>
    public string? Reason { get; set; }
}