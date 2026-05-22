using poscam.AuthServer.Models.Dtos.Common;

namespace poscam.AuthServer.Models.Dtos.Viewer;

/// <summary>
/// 캠뷰어 로그인 응답 DTO.
/// </summary>
public class ViewerLoginResponse
{
    /// <summary>
    /// 매장 코드.
    /// </summary>
    public int StoreCode { get; set; }

    /// <summary>
    /// 등록 또는 확인된 캠뷰어 장비 코드.
    /// </summary>
    public int DeviceCode { get; set; }

    /// <summary>
    /// 로그인 성공 여부.
    /// </summary>
    public bool LoginSuccess { get; set; }

    /// <summary>
    /// 서버 설정 버전.
    /// 캠뷰어 로컬 설정 파일과 비교할 때 사용한다.
    /// </summary>
    public string ConfigVersion { get; set; } = "";

    /// <summary>
    /// 서버가 발급한 캠뷰어 인증 토큰.
    /// </summary>
    public AuthTokenDto Token { get; set; } = new();
}