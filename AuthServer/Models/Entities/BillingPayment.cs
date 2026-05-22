namespace poscam.AuthServer.Models.Entities
{
    /// <summary>
    /// 파트너사별 월 납부 처리 Entity.
    ///
    /// DB 테이블: billing_payment
    ///
    /// 역할:
    /// - 파트너사 + 청구월 단위의 납부 상태를 관리한다.
    /// - 계약별 청구내역(contract_billing)을 파트너사별로 합산한 금액을 기준으로 처리한다.
    /// </summary>
    public class BillingPayment
    {
        /// <summary>
        /// 납부 처리 코드.
        /// DB 컬럼: pay_code
        /// </summary>
        public int PayCode { get; set; }

        /// <summary>
        /// 청구월.
        /// 예: 202605
        /// DB 컬럼: bill_month
        /// </summary>
        public int BillMonth { get; set; }

        /// <summary>
        /// 파트너사 코드.
        /// DB 컬럼: partner_code
        /// </summary>
        public int PartnerCode { get; set; }

        /// <summary>
        /// 청구금액 합계.
        /// DB 컬럼: pay_bill_amount
        /// </summary>
        public int PayBillAmount { get; set; }

        /// <summary>
        /// 실제 납부금액.
        /// DB 컬럼: pay_amount
        /// </summary>
        public int PayAmount { get; set; }

        /// <summary>
        /// 미납금액.
        /// DB 컬럼: pay_remain_amount
        /// </summary>
        public int PayRemainAmount { get; set; }

        /// <summary>
        /// 납부 상태.
        /// 0=미처리, 1=미납, 2=부분납부, 3=납부완료, 4=보류, 9=취소.
        /// DB 컬럼: pay_status
        /// </summary>
        public int PayStatus { get; set; }

        /// <summary>
        /// 납부일.
        /// DB 컬럼: pay_date
        /// </summary>
        public DateTime? PayDate { get; set; }

        /// <summary>
        /// 납부 방식.
        /// DB 컬럼: pay_method
        /// </summary>
        public string? PayMethod { get; set; }

        /// <summary>
        /// 처리 메모.
        /// DB 컬럼: pay_memo
        /// </summary>
        public string? PayMemo { get; set; }

        /// <summary>
        /// 처리한 관리자 user_code.
        /// DB 컬럼: pay_created_by
        /// </summary>
        public int? PayCreatedBy { get; set; }

        /// <summary>
        /// 등록일.
        /// DB 컬럼: pay_rdate
        /// </summary>
        public DateTime PayRdate { get; set; }

        /// <summary>
        /// 수정일.
        /// DB 컬럼: pay_udate
        /// </summary>
        public DateTime? PayUdate { get; set; }
    }
}
