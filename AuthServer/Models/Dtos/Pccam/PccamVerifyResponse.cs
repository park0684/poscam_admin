using poscam.AuthServer.Models.Dtos.Common;

namespace poscam.AuthServer.Models.Dtos.Pccam;

/// <summary>
/// PC캠 실행 인증 응답 DTO.
/// </summary>
public class PccamVerifyResponse
{
    /// <summary>
    /// 인증 유효 여부.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 토큰 및 장비정보를 통해 확인한 매장 코드.
    /// 매장 없이 생성된 계약으로 인증된 장비는 null을 반환한다.
    /// </summary>
    public int? StoreCode { get; set; }

    /// <summary>
    /// 장비 코드.
    /// </summary>
    public int DeviceCode { get; set; }

    /// <summary>
    /// 갱신된 인증 토큰.
    /// 서버 재인증 성공 시 새 토큰을 반환한다.
    /// </summary>
    public AuthTokenDto Token { get; set; } = new();
}