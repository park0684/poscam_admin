namespace poscam.AdminWeb.Models.Store;

/// <summary>
/// 매장 등록/수정 요청 DTO.
/// </summary>
public class StoreSaveRequest
{
    public int? StoreCode { get; set; }

    public string StoreName { get; set; } = "";

    public string? StoreBizNum { get; set; }

    public string? StoreOwnerName { get; set; }

    public string? StoreTel { get; set; }

    public string? StoreEmail { get; set; }

    public string? StoreZipcode { get; set; }

    public string? StoreAddress1 { get; set; }

    public string? StoreAddress2 { get; set; }

    public string? StoreMemo { get; set; }

    public int? StoreStatus { get; set; } = 1;

    /// <summary>
    /// 매장 등록 시 대표 파트너사.
    /// 관리자: 선택 가능.
    /// 담당자: 본인 PartnerCode로 고정.
    /// </summary>
    public int? PrimaryPartnerCode { get; set; }

    /// <summary>
    /// 매장 등록 시 대표 관리 담당자.
    /// 선택된 파트너사 내 담당자만 가능.
    /// 담당자가 직접 등록할 경우 미선택 시 본인으로 처리 가능.
    /// </summary>
    public int? PrimaryManagerUserCode { get; set; }
}