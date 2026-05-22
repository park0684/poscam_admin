namespace poscam.AdminWeb.Models.License;

public class LicenseRestoreResponse
{
    public int LicenseCode { get; set; }

    public int ContractCode { get; set; }

    public int? StoreCode { get; set; }

    public int RestoredStatus { get; set; }

    public bool Restored { get; set; }
}