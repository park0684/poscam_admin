namespace poscam.AuthServer.Models.Dtos.Settlement
{
    /// <summary>
    /// 파트너사별 월 정산 합산 DTO.
    ///
    /// contract_billing을 bill_month + partner_code 기준으로 합산해서 사용한다.
    /// </summary>
    public class PartnerMonthlySettlementDto
    {
        public int BillMonth { get; set; }

        public int PartnerCode { get; set; }

        public string? PartnerName { get; set; }

        public int ContractCount { get; set; }

        public int PccamTotalCount { get; set; }

        public int ViewerTotalCount { get; set; }

        public int PccamTotalAmount { get; set; }

        public int ViewerTotalAmount { get; set; }

        public int TotalAmount { get; set; }

        public int PaidAmount { get; set; }

        public int RemainAmount { get; set; }

        public int PaymentStatus { get; set; }
    }
}
