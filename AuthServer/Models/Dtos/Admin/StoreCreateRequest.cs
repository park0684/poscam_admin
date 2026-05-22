namespace poscam.AuthServer.Models.Dtos.Admin;

/// <summary>
/// 매장 등록 요청 DTO.
/// 
/// 관리자 페이지에서 신규 매장을 등록할 때 사용한다.
/// </summary>
public class StoreCreateRequest
{
    /// <summary>
    /// 매장명.
    /// </summary>
    public string StoreName { get; set; } = "";

    /// <summary>
    /// 사업자번호.
    /// </summary>
    public string? StoreBizNum { get; set; }
}