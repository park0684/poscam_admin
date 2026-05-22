namespace poscam.AuthServer.Models.Dtos.Settlement;

/// <summary>
/// 계약 청구자료 확정 요청 DTO.
///
/// 청구월 기준으로 생성된 계약 청구자료를 청구확정 상태로 변경한다.
/// partnerCode가 null이면 해당 청구월 전체 파트너사를 대상으로 한다.
/// </summary>
public class ContractChargeConfirmRequest
{
    /// <summary>
    /// 청구월.
    /// 예: 202605
    /// </summary>
    public int BillMonth { get; set; }

    /// <summary>
    /// 특정 파트너사만 확정할 경우 사용.
    /// null이면 전체 파트너사 대상.
    /// </summary>
    public int? PartnerCode { get; set; }

    /// <summary>
    /// 처리 메모.
    /// </summary>
    public string? Memo { get; set; }
}