namespace poscam.AdminWeb.Models.Store;

/// <summary>
/// 매장 목록 표시 DTO.
/// 
/// AuthServer의 StoreListItemDto와 구조를 맞춘다.
/// </summary>
public class StoreListItemDto
{
    /// <summary>
    /// 매장 내부 코드.
    /// 상세 조회 및 팝업 호출에 사용한다.
    /// </summary>
    public int StoreCode { get; set; }

    /// <summary>
    /// 매장 ID.
    /// 화면에 표시되는 외부 식별값이다.
    /// </summary>
    public string StoreId { get; set; } = "";

    /// <summary>
    /// 매장명.
    /// </summary>
    public string StoreName { get; set; } = "";

    /// <summary>
    /// 사업자번호.
    /// </summary>
    public string? StoreBizNum { get; set; }

    /// <summary>
    /// 대표자명.
    /// </summary>
    public string? StoreOwnerName { get; set; }

    /// <summary>
    /// 매장 전화번호.
    /// </summary>
    public string? StoreTel { get; set; }

    /// <summary>
    /// 기본 주소.
    /// </summary>
    public string? StoreAddress1 { get; set; }

    /// <summary>
    /// 상세 주소.
    /// </summary>
    public string? StoreAddress2 { get; set; }

    /// <summary>
    /// 매장 상태.
    /// StoreStatus enum 값.
    /// </summary>
    public int StoreStatus { get; set; }

    /// <summary>
    /// 담당 파트너사명.
    /// </summary>
    public string? PrimaryPartnerName { get; set; }

    /// <summary>
    /// 담당자명.
    /// </summary>
    public string? PrimaryUserName { get; set; }

    /// <summary>
    /// 계약 건수.
    /// </summary>
    public int ContractCount { get; set; }

    /// <summary>
    /// PC캠 계약 수량.
    /// contracts.con_pcc 합계.
    /// </summary>
    public int PccamContractCount { get; set; }

    /// <summary>
    /// 캠뷰어 계약 수량.
    /// contracts.con_view 합계.
    /// </summary>
    public int ViewerContractCount { get; set; }

    /// <summary>
    /// 실제 등록된 PC캠 장비 수량.
    /// 이번 매장 목록 화면에서는 계약수량과 구분된다.
    /// </summary>
    public int PccamDeviceCount { get; set; }

    /// <summary>
    /// 실제 등록된 캠뷰어 장비 수량.
    /// 이번 매장 목록 화면에서는 계약수량과 구분된다.
    /// </summary>
    public int ViewerDeviceCount { get; set; }

    /// <summary>
    /// 매장 등록일.
    /// </summary>
    public DateTime? RegisteredAt { get; set; }
}