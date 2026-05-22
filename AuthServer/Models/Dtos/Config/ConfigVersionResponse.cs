namespace poscam.AuthServer.Models.Dtos.Config;

/// <summary>
/// 설정 버전 조회 응답 DTO.
/// </summary>
public class ConfigVersionResponse
{
    /// <summary>
    /// 매장 코드.
    /// </summary>
    public int StoreCode { get; set; }

    /// <summary>
    /// 서버에 저장된 설정 버전.
    /// </summary>
    public string ConfigVersion { get; set; } = "";

    /// <summary>
    /// 클라이언트 로컬 설정이 최신인지 여부.
    /// </summary>
    public bool IsLatest { get; set; }
}