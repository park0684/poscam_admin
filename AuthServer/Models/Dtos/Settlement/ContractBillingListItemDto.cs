namespace poscam.AuthServer.Models.Dtos.Settlement
{
    /// <summary>
    /// 계약별 월 청구내역 목록 DTO.
    /// </summary>
    public class ContractBillingListItemDto
    {
        public int BillCode { get; set; }

        public int BillMonth { get; set; }

        public int PartnerCode { get; set; }

        public string? PartnerName { get; set; }

        public int StoreCode { get; set; }

        public string? StoreName { get; set; }

        public int ContractCode { get; set; }

        public string? ContractNo { get; set; }

        public int BillPccamCount { get; set; }

        public int BillViewerCount { get; set; }

        public int BillPccamUnitPrice { get; set; }

        public int BillViewerUnitPrice { get; set; }

        public int BillPccamAmount { get; set; }

        public int BillViewerAmount { get; set; }

        public int BillTotalAmount { get; set; }

        public int BillStatus { get; set; }

        public int PaymentStatus { get; set; }

        public string? BillMemo { get; set; }

        public DateTime BillRdate { get; set; }

        public DateTime? BillUdate { get; set; }
    }
}
