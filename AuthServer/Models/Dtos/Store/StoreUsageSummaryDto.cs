namespace poscam.AuthServer.Models.Dtos.Store
{
    public class StoreUsageSummaryDto
    {
        public int StoreCode { get; set; }

        public int PccamUseCount { get; set; }

        public int ViewerUseCount { get; set; }
    }
}
