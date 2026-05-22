namespace poscam.AdminWeb.Models.Contract;

/// <summary>
/// 계약 등록/수정 응답 DTO.
/// </summary>
public class ContractSaveResponse
{
    public int ContractCode { get; set; }

    public int StoreCode { get; set; }

    public string ContractNo { get; set; } = "";

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool Created { get; set; }

    public bool Saved { get; set; }
}