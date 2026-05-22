namespace poscam.AuthServer.Models.Dtos.Settlement
{
    /// <summary>
    /// 파트너사별 월 납부 처리 저장 요청 DTO.
    /// </summary>
    public class BillingPaymentSaveRequest
    {
        /// <summary>
        /// 납부 처리 코드.
        /// 신규 저장 시 0 가능.
        /// </summary>
        public int PayCode { get; set; }

        /// <summary>
        /// 청구월.
        /// 예: 202605
        /// </summary>
        public int BillMonth { get; set; }

        /// <summary>
        /// 파트너사 코드.
        /// </summary>
        public int PartnerCode { get; set; }

        /// <summary>
        /// 실제 납부금액.
        /// </summary>
        public int PayAmount { get; set; }

        /// <summary>
        /// 납부 상태.
        /// 0=미처리, 1=미납, 2=부분납부, 3=납부완료, 4=보류, 9=취소.
        /// </summary>
        public int PayStatus { get; set; }

        /// <summary>
        /// 납부일.
        /// </summary>
        public DateTime? PayDate { get; set; }

        /// <summary>
        /// 납부 방식.
        /// 예: 계좌이체, 현금, 카드 등.
        /// </summary>
        public string? PayMethod { get; set; }

        /// <summary>
        /// 처리 메모.
        /// </summary>
        public string? Memo { get; set; }
    }
}
