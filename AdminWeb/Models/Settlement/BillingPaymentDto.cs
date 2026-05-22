namespace poscam.AdminWeb.Models.Settlement
{
    /// <summary>
    /// 파트너사별 월 납부 처리 DTO.
    ///
    /// AuthServer API:
    /// GET /api/manage/settlements/payments
    /// </summary>
    public class BillingPaymentDto
    {
        public int PayCode { get; set; }

        public int BillMonth { get; set; }

        public int PartnerCode { get; set; }

        public string? PartnerName { get; set; }

        public int PayBillAmount { get; set; }

        public int PayAmount { get; set; }

        public int PayRemainAmount { get; set; }

        public int PayStatus { get; set; }

        public DateTime? PayDate { get; set; }

        public string? PayMethod { get; set; }

        public string? PayMemo { get; set; }

        public int? PayCreatedBy { get; set; }

        public DateTime PayRdate { get; set; }

        public DateTime? PayUdate { get; set; }
    }
}
