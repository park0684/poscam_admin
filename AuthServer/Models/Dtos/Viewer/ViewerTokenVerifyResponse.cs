using poscam.AuthServer.Models.Dtos.Common;

namespace poscam.AuthServer.Models.Dtos.Viewer;

/// <summary>
/// 캠뷰어 토큰 실행 인증 응답 DTO.
/// </summary>
public class ViewerTokenVerifyResponse
{
    /// <summary>
    /// 실행 가능 여부.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 매장 코드.
    /// </summary>
    public int StoreCode { get; set; }

    /// <summary>
    /// 등록 장비 코드.
    /// </summary>
    public int DeviceCode { get; set; }

    /// <summary>
    /// 서버 설정 버전.
    /// </summary>
    public string ConfigVersion { get; set; } = "";

    /// <summary>
    /// 갱신된 토큰.
    /// </summary>
    public AuthTokenDto Token { get; set; } = new();
}