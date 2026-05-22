namespace poscam.AdminWeb.Models.Settlement
{
    public class ContractChargeConfirmRequest
    {
        public int BillMonth { get; set; }

        public int? PartnerCode { get; set; }

        public string? Memo { get; set; }
    }
}
