namespace poscam.AdminWeb.Models.Store;

public class StoreUsageSummaryDto
{
    public int StoreCode { get; set; }

    public int PccamUseCount { get; set; }

    public int ViewerUseCount { get; set; }
}