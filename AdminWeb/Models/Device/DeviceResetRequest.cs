namespace poscam.AdminWeb.Models.Device;

/// <summary>
/// 장비 초기화 요청 DTO.
/// </summary>
public class DeviceResetRequest
{
    public int DeviceCode { get; set; }

    public string? Reason { get; set; }
}