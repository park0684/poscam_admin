namespace poscam.AuthServer.Models.Dtos.Device;

/// <summary>
/// 관리자 장비 초기화 응답 DTO.
/// </summary>
public class DeviceResetResponse
{
    /// <summary>
    /// 초기화된 장비 코드.
    /// </summary>
    public int DeviceCode { get; set; }

    /// <summary>
    /// 초기화 성공 여부.
    /// </summary>
    public bool ResetSuccess { get; set; }
}