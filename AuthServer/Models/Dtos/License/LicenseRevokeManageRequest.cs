namespace poscam.AuthServer.Models.Dtos.License;

/// <summary>
/// 관리자/담당자용 인증키 폐기 요청 DTO.
/// </summary>
public class LicenseRevokeManageRequest
{
    /// <summary>
    /// 폐기 사유.
    /// 비어 있으면 기본 사유를 사용한다.
    /// </summary>
    public string? Reason { get; set; }
}