namespace poscam.AuthServer.Models.Dtos.Store;

/// <summary>
/// 매장 상세 화면의 장비 정보 DTO.
/// </summary>
public class StoreDeviceDto
{
    public int DeviceCode { get; set; }

    public int StoreCode { get; set; }

    public int? LicenseCode { get; set; }

    public int AppType { get; set; }

    public string HwidMasked { get; set; } = "";

    public int PosNo { get; set; }

    public string? DeviceName { get; set; }

    public DateTime? RegisteredAt { get; set; }
}