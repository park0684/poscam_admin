namespace poscam.AdminWeb.Models.Device;

/// <summary>
/// 장비 초기화 응답 DTO.
/// </summary>
public class DeviceResetResponse
{
    public int DeviceCode { get; set; }

    public bool ResetSuccess { get; set; }
}