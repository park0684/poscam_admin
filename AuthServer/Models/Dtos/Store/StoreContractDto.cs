namespace poscam.AuthServer.Models.Dtos.Store;

/// <summary>
/// 매장 상세 화면의 계약 정보 DTO.
/// </summary>
public class StoreContractDto
{
    public int ContractCode { get; set; }

    public int StoreCode { get; set; }

    public string ContractNo { get; set; } = "";

    public int ContractType { get; set; }

    public int PccamCount { get; set; }

    public int ViewerCount { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int Status { get; set; }

    public DateTime RegisteredAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}