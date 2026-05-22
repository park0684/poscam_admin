namespace poscam.AdminWeb.Models.Settlement
{
    /// <summary>
    /// 파트너사 단가 정책 등록/수정 요청 DTO.
    ///
    /// AuthServer API:
    /// POST /api/manage/settlements/price-policies
    /// </summary>
    public class PartnerPricePolicySaveRequest
    {
        /// <summary>
        /// 단가 정책 코드.
        /// 신규 등록 시 0.
        /// 수정 시 기존 ppp_code.
        /// </summary>
        public int PppCode { get; set; }

        /// <summary>
        /// 파트너사 코드.
        /// </summary>
        public int PartnerCode { get; set; }

        /// <summary>
        /// PC캠 월 단가.
        /// </summary>
        public int PccamPrice { get; set; }

        /// <summary>
        /// 캠뷰어 월 단가.
        /// </summary>
        public int ViewerPrice { get; set; }

        /// <summary>
        /// 적용 시작월.
        /// 예: 202605
        /// </summary>
        public int StartMonth { get; set; }

        /// <summary>
        /// 적용 종료월.
        /// NULL이면 종료 없음.
        /// </summary>
        public int? EndMonth { get; set; }

        /// <summary>
        /// 상태.
        /// 1=사용, 0=미사용.
        /// </summary>
        public int Status { get; set; } = 1;

        /// <summary>
        /// 메모.
        /// </summary>
        public string? Memo { get; set; }
    }
}
