namespace poscam.AuthServer.Models.Entities
{
    /// <summary>
    /// 계약별 월 청구자료 Entity.
    ///
    /// DB 테이블: contract_billing
    ///
    /// 역할:
    /// - 특정 청구월의 계약별 청구 스냅샷을 저장한다.
    /// - 계약 수량과 파트너 단가를 생성 시점 기준으로 저장한다.
    /// - 이후 계약 수량 또는 단가가 변경되어도 기존 청구금액이 흔들리지 않도록 한다.
    /// </summary>
    public class ContractBilling
    {
        /// <summary>
        /// 청구 코드.
        /// DB 컬럼: bill_code
        /// </summary>
        public int BillCode { get; set; }

        /// <summary>
        /// 청구월.
        /// 예: 202605
        /// DB 컬럼: bill_month
        /// </summary>
        public int BillMonth { get; set; }

        /// <summary>
        /// 청구 대상 파트너사 코드.
        /// DB 컬럼: partner_code
        /// </summary>
        public int PartnerCode { get; set; }

        /// <summary>
        /// 매장 코드.
        /// DB 컬럼: store_code
        /// </summary>
        public int StoreCode { get; set; }

        /// <summary>
        /// 계약 코드.
        /// DB 컬럼: contract_code
        /// </summary>
        public int ContractCode { get; set; }

        /// <summary>
        /// 계약번호 스냅샷.
        /// DB 컬럼: contract_no
        /// </summary>
        public string? ContractNo { get; set; }

        /// <summary>
        /// 계약서 기준 PC캠 수량.
        /// DB 컬럼: bill_pccam_count
        /// </summary>
        public int BillPccamCount { get; set; }

        /// <summary>
        /// 계약서 기준 캠뷰어 수량.
        /// DB 컬럼: bill_viewer_count
        /// </summary>
        public int BillViewerCount { get; set; }

        /// <summary>
        /// 적용 PC캠 단가.
        /// DB 컬럼: bill_pccam_unit_price
        /// </summary>
        public int BillPccamUnitPrice { get; set; }

        /// <summary>
        /// 적용 캠뷰어 단가.
        /// DB 컬럼: bill_viewer_unit_price
        /// </summary>
        public int BillViewerUnitPrice { get; set; }

        /// <summary>
        /// PC캠 청구금액.
        /// DB 컬럼: bill_pccam_amount
        /// </summary>
        public int BillPccamAmount { get; set; }

        /// <summary>
        /// 캠뷰어 청구금액.
        /// DB 컬럼: bill_viewer_amount
        /// </summary>
        public int BillViewerAmount { get; set; }

        /// <summary>
        /// 총 청구금액.
        /// DB 컬럼: bill_total_amount
        /// </summary>
        public int BillTotalAmount { get; set; }

        /// <summary>
        /// 청구 상태.
        /// 0=생성대기, 1=청구대기, 2=청구확정, 9=취소.
        /// DB 컬럼: bill_status
        /// </summary>
        public int BillStatus { get; set; }

        /// <summary>
        /// 납부 상태.
        /// 0=미처리, 1=미납, 2=부분납부, 3=납부완료, 4=보류, 9=취소.
        /// DB 컬럼: payment_status
        /// </summary>
        public int PaymentStatus { get; set; }

        /// <summary>
        /// 청구 메모.
        /// DB 컬럼: bill_memo
        /// </summary>
        public string? BillMemo { get; set; }

        /// <summary>
        /// 생성일.
        /// DB 컬럼: bill_rdate
        /// </summary>
        public DateTime BillRdate { get; set; }

        /// <summary>
        /// 수정일.
        /// DB 컬럼: bill_udate
        /// </summary>
        public DateTime? BillUdate { get; set; }
    }
}
