namespace poscam.AdminWeb.Models.Settlement
{
    public class ContractChargeStatusChangeResponse
    {
        public int BillMonth { get; set; }

        public int? PartnerCode { get; set; }

        public int ChangedCount { get; set; }

        public int NewBillStatus { get; set; }

        public string Message { get; set; } = "";
    }
}
