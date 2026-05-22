namespace poscam.AuthServer.Models.Dtos.Settlement
{
    /// <summary>
    /// 월별 청구자료 생성 결과 DTO.
    /// </summary>
    public class BillingGenerateResponse
    {
        public int BillMonth { get; set; }

        public int CreatedCount { get; set; }

        public int SkippedCount { get; set; }

        public int TotalAmount { get; set; }

        public List<string> Messages { get; set; } = new();
    }
}
