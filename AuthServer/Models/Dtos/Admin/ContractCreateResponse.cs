namespace poscam.AuthServer.Models.Dtos.Admin;

/// <summary>
/// 계약 등록 응답 DTO.
/// </summary>
public class ContractCreateResponse
{
    /// <summary>
    /// 생성된 계약 코드.
    /// </summary>
    public int ContractCode { get; set; }

    /// <summary>
    /// 계약이 연결된 매장 코드.
    /// </summary>
    public int StoreCode { get; set; }

    /// <summary>
    /// 계약 번호.
    /// </summary>
    public string ContractNo { get; set; } = "";
}