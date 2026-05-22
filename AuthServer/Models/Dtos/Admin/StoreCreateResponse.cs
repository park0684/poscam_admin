namespace poscam.AuthServer.Models.Dtos.Admin;

/// <summary>
/// 매장 등록 응답 DTO.
/// </summary>
public class StoreCreateResponse
{
    /// <summary>
    /// 생성된 매장 코드.
    /// </summary>
    public int StoreCode { get; set; }

    /// <summary>
    /// 매장 로그인 ID.
    /// </summary>
    public string StoreId { get; set; } = "";

    /// <summary>
    /// 초기 비밀번호
    /// </summary>
    public string InitialPassword { get; set; } = "";

    /// <summary>
    /// 매장명.
    /// </summary>
    public string StoreName { get; set; } = "";
}