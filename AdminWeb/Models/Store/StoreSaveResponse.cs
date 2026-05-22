namespace poscam.AdminWeb.Models.Store;

/// <summary>
/// 매장 등록/수정 응답 DTO.
/// </summary>
public class StoreSaveResponse
{
    public int StoreCode { get; set; }

    public string StoreId { get; set; } = "";

    public string? InitialPassword { get; set; }

    public string StoreName { get; set; } = "";

    public bool Created { get; set; }

    public bool Saved { get; set; }
}