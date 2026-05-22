namespace poscam.AuthServer.Models.Dtos.Settlement
{
    /// <summary>
    /// 계약 청구자료 상태 변경 응답 DTO.
    /// </summary>
    public class ContractChargeStatusChangeResponse
    {
        public int BillMonth { get; set; }

        public int? PartnerCode { get; set; }

        public int ChangedCount { get; set; }

        /// <summary>
        /// 변경된 청구 상태.
        /// 1=청구대기, 2=청구확정, 9=취소.
        /// </summary>
        public int NewBillStatus { get; set; }

        public string Message { get; set; } = "";
    }
}
