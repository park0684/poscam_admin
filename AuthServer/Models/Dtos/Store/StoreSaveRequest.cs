namespace poscam.AuthServer.Models.Dtos.Store;

/// <summary>
/// 매장 등록/수정 요청 DTO.
/// 
/// 신규 등록 시 StoreCode는 null 또는 0이다.
/// 수정 시 StoreCode가 필요하다.
/// 매장 ID는 백엔드에서 자동 생성하므로 요청에 포함하지 않는다.
/// </summary>
public class StoreSaveRequest
{
    /// <summary>
    /// 매장 코드.
    /// 신규 등록 시 null 또는 0.
    /// </summary>
    public int? StoreCode { get; set; }

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
    /// 매장 연락처.
    /// </summary>
    public string? StoreTel { get; set; }

    /// <summary>
    /// 매장 이메일.
    /// </summary>
    public string? StoreEmail { get; set; }

    /// <summary>
    /// 우편번호.
    /// </summary>
    public string? StoreZipcode { get; set; }

    /// <summary>
    /// 기본 주소.
    /// </summary>
    public string? StoreAddress1 { get; set; }

    /// <summary>
    /// 상세 주소.
    /// </summary>
    public string? StoreAddress2 { get; set; }

    /// <summary>
    /// 매장 메모.
    /// </summary>
    public string? StoreMemo { get; set; }

    /// <summary>
    /// 매장 상태.
    /// 신규 등록 시 생략하면 Active로 처리할 수 있다.
    /// </summary>
    public int? StoreStatus { get; set; }

    /// <summary>
    /// 매장 등록 시 대표 파트너사.
    /// 관리자: 선택 가능.
    /// 담당자: 본인 PartnerCode로 강제된다.
    /// </summary>
    public int? PrimaryPartnerCode { get; set; }

    /// <summary>
    /// 매장 등록 시 대표 관리 담당자.
    /// 선택된 파트너사 내 담당자만 가능하다.
    /// 담당자가 직접 등록할 경우 미지정 시 본인을 대표 담당자로 지정할 수 있다.
    /// </summary>
    public int? PrimaryManagerUserCode { get; set; }
}