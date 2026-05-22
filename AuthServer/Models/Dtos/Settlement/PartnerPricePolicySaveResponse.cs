namespace poscam.AuthServer.Models.Dtos.Settlement
{
    /// <summary>
    /// 파트너사 단가 정책 저장 응답 DTO.
    /// </summary>
    public class PartnerPricePolicySaveResponse
    {
        public int PppCode { get; set; }

        public int PartnerCode { get; set; }

        public bool Created { get; set; }

        public bool Saved { get; set; }
    }
}
