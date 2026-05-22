namespace poscam.AuthServer.Models.Dtos.Store;

/// <summary>
/// 매장 상세 기본정보 DTO.
/// </summary>
public class StoreDetailDto
{
    public int StoreCode { get; set; }

    public string StoreId { get; set; } = "";

    public string StoreName { get; set; } = "";

    public string? StoreBizNum { get; set; }

    public string? StoreOwnerName { get; set; }

    public string? StoreTel { get; set; }

    public string? StoreEmail { get; set; }

    public string? StoreZipcode { get; set; }

    public string? StoreAddress1 { get; set; }

    public string? StoreAddress2 { get; set; }

    public string? StoreMemo { get; set; }

    public int StoreStatus { get; set; }

    public DateTime? RegisteredAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}