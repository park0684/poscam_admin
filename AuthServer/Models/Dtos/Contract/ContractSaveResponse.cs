namespace poscam.AuthServer.Models.Dtos.Contract;

/// <summary>
/// 관리자 화면의 계약 등록/수정 응답 DTO.
/// 
/// 계약은 파트너사 기준으로 관리되며,
/// 매장은 선택적으로 연결될 수 있다.
/// </summary>
public class ContractSaveResponse
{
    /// <summary>
    /// 계약 코드.
    /// </summary>
    public int ContractCode { get; set; }

    /// <summary>
    /// 계약과 연결된 매장 코드.
    /// 매장 없이 생성된 계약은 null.
    /// </summary>
    public int? StoreCode { get; set; }

    /// <summary>
    /// 계약 번호.
    /// </summary>
    public string ContractNo { get; set; } = "";

    /// <summary>
    /// 계약 시작일.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 계약 종료일.
    /// 구매형 계약은 null 가능.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 신규 등록 여부.
    /// </summary>
    public bool Created { get; set; }

    /// <summary>
    /// 저장 성공 여부.
    /// </summary>
    public bool Saved { get; set; }
}