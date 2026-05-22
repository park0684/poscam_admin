namespace poscam.AuthServer.Models.Dtos.Settlement
{
    /// <summary>
    /// 정산 생성을 위한 계약 조회 DTO.
    ///
    /// DB에서 계약, 매장, 파트너 정보를 조합해 월별 청구자료 생성에 필요한 최소 정보만 가져온다.
    /// 실제 프로젝트에서는 Models/Dtos/Settlement/BillingTargetContractDto.cs 로 분리하는 것을 권장한다.
    /// </summary>
    public class BillingTargetContractDto
    {
        /// <summary>
        /// 계약 코드.
        /// DB 예: contracts.contract_code
        /// </summary>
        public int ContractCode { get; set; }

        /// <summary>
        /// 계약번호.
        /// DB 예: contracts.contract_no 또는 con_no
        /// </summary>
        public string? ContractNo { get; set; }

        /// <summary>
        /// 매장 코드.
        /// DB 예: contracts.con_store 또는 store_code
        /// </summary>
        public int StoreCode { get; set; }

        /// <summary>
        /// 매장명.
        /// </summary>
        public string? StoreName { get; set; }

        /// <summary>
        /// 정산 대상 파트너사 코드.
        ///
        /// 현재 정책상 계약이 속한 파트너사의 기준단가로 정산한다.
        /// 계약 테이블에 partner_code가 있으면 계약 기준을 사용하고,
        /// 없으면 매장의 관리/담당 파트너 기준으로 가져오도록 SQL을 조정한다.
        /// </summary>
        public int PartnerCode { get; set; }

        /// <summary>
        /// 파트너사명.
        /// </summary>
        public string? PartnerName { get; set; }

        /// <summary>
        /// 계약서에 등록된 PC캠 수량.
        /// 정산 수량 기준이다.
        /// </summary>
        public int PccamCount { get; set; }

        /// <summary>
        /// 계약서에 등록된 캠뷰어 수량.
        /// 정산 수량 기준이다.
        /// </summary>
        public int ViewerCount { get; set; }

        /// <summary>
        /// 계약 상태.
        /// 정상 계약만 청구 대상에 포함한다.
        /// </summary>
        public int ContractStatus { get; set; }

        /// <summary>
        /// 계약 시작일.
        /// 청구월 기준 유효 계약 판단에 사용한다.
        /// </summary>
        public DateTime? ContractStartDate { get; set; }

        /// <summary>
        /// 계약 종료일.
        /// NULL이면 종료일 없음으로 본다.
        /// </summary>
        public DateTime? ContractEndDate { get; set; }
    }

    

}

