namespace poscam.AuthServer.Models.Dtos.Store;

/// <summary>
/// 매장 상세 화면의 라이선스 정보 DTO.
/// </summary>
public class StoreLicenseDto
{
    public int LicenseCode { get; set; }

    public int ContractCode { get; set; }

    public string ContractNo { get; set; } = "";

    public string LicenseKey { get; set; } = "";

    public int LicenseStatus { get; set; }

    public int? RegisteredDeviceCode { get; set; }

    public string? RegisteredHwidMasked { get; set; }

    public int? PosNo { get; set; }

    public DateTime RegisteredAt { get; set; }
}