namespace poscam.AuthServer.Models.Dtos.License;

/// <summary>
/// 인증키 복구 요청 DTO.
/// </summary>
public class LicenseRestoreManageRequest
{
    /// <summary>
    /// 복구 사유.
    /// </summary>
    public string? Reason { get; set; }
}