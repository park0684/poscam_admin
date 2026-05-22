namespace poscam.AuthServer.Models.Dtos.Store;

/// <summary>
/// 매장 목록 표시 DTO.
/// 
/// 관리자 화면의 매장 목록 그리드에서 사용한다.
/// </summary>
public class StoreListItemDto
{
    public int StoreCode { get; set; }

    public string StoreId { get; set; } = "";

    public string StoreName { get; set; } = "";

    public string? StoreBizNum { get; set; }

    public string? StoreOwnerName { get; set; }

    public string? StoreTel { get; set; }

    public string? StoreAddress1 { get; set; }

    public string? StoreAddress2 { get; set; }

    public int StoreStatus { get; set; }

    public string? PrimaryPartnerName { get; set; }

    public string? PrimaryUserName { get; set; }

    public int ContractCount { get; set; }

    public int PccamDeviceCount { get; set; }

    public int ViewerDeviceCount { get; set; }

    public DateTime? RegisteredAt { get; set; }
}