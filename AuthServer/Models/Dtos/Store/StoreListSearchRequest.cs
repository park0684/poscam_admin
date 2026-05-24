namespace poscam.AuthServer.Models.Dtos.Store;

/// <summary>
/// 매장 목록 검색 조건 DTO.
/// 
/// 사용 위치:
/// - GET /api/manage/stores
/// 
/// 검색 조건:
/// - 매장 상태
/// - 담당 파트너
/// - 등록일 범위
/// - 계약일 범위
/// - 매장 ID / 매장명 검색어
/// </summary>
public class StoreListSearchRequest
{
    /// <summary>
    /// 매장 상태.
    /// null이면 전체 상태를 조회한다.
    /// 화면 기본값은 Active(1)이다.
    /// </summary>
    public int? StoreStatus { get; set; }

    /// <summary>
    /// 담당 파트너 코드.
    /// System/Admin 조회 시 사용한다.
    /// </summary>
    public int? PartnerCode { get; set; }

    /// <summary>
    /// 매장 등록일 시작일.
    /// stores.store_rdate 기준.
    /// </summary>
    public DateTime? RegisteredFrom { get; set; }

    /// <summary>
    /// 매장 등록일 종료일.
    /// stores.store_rdate 기준.
    /// </summary>
    public DateTime? RegisteredTo { get; set; }

    /// <summary>
    /// 계약일 시작일.
    /// 현재 contracts.con_start 기준으로 조회한다.
    /// 실제 계약 등록일 기준이 필요하면 con_rdate로 변경한다.
    /// </summary>
    public DateTime? ContractFrom { get; set; }

    /// <summary>
    /// 계약일 종료일.
    /// 현재 contracts.con_start 기준으로 조회한다.
    /// </summary>
    public DateTime? ContractTo { get; set; }

    /// <summary>
    /// 검색어.
    /// 매장 ID 또는 매장명 검색에 사용한다.
    /// </summary>
    public string? Keyword { get; set; }
}