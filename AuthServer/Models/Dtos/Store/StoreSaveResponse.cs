namespace poscam.AuthServer.Models.Dtos.Store;

/// <summary>
/// 매장 등록/수정 응답 DTO.
/// </summary>
public class StoreSaveResponse
{
    /// <summary>
    /// 매장 코드.
    /// 신규 등록 후 이 값을 프론트엔드가 보관해야 한다.
    /// </summary>
    public int StoreCode { get; set; }

    /// <summary>
    /// 자동 생성된 매장 ID.
    /// </summary>
    public string StoreId { get; set; } = "";

    /// <summary>
    /// 신규 등록 시 최초 비밀번호.
    /// 수정 시에는 null일 수 있다.
    /// </summary>
    public string? InitialPassword { get; set; }

    /// <summary>
    /// 매장명.
    /// </summary>
    public string StoreName { get; set; } = "";

    /// <summary>
    /// 신규 등록 여부.
    /// </summary>
    public bool Created { get; set; }

    /// <summary>
    /// 저장 완료 여부.
    /// </summary>
    public bool Saved { get; set; }
}