namespace poscam.AdminWeb.Models.Settlement
{
    /// <summary>
    /// 월별 청구자료 생성 요청 DTO.
    ///
    /// AuthServer API:
    /// POST /api/manage/settlements/billings/generate
    /// </summary>
    public class BillingGenerateRequest
    {
        /// <summary>
        /// 청구월.
        /// 예: 202605
        /// </summary>
        public int BillMonth { get; set; }

        /// <summary>
        /// 특정 파트너사만 생성할 경우 사용.
        /// NULL이면 전체 파트너사 대상.
        /// </summary>
        public int? PartnerCode { get; set; }

        /// <summary>
        /// 청구대기 상태 자료를 삭제 후 재생성할지 여부.
        /// </summary>
        public bool RegeneratePending { get; set; }
    }
}
