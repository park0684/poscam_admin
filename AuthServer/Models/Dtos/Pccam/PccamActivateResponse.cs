using poscam.AuthServer.Models.Dtos.Common;

namespace poscam.AuthServer.Models.Dtos.Pccam;

/// <summary>
/// PC캠 최초 인증 응답 DTO.
/// </summary>
public class PccamActivateResponse
{
    /// <summary>
    /// 등록된 장비 코드.
    /// </summary>
    public int DeviceCode { get; set; }

    /// <summary>
    /// 매장 코드.
    /// 매장 없이 생성된 계약으로 인증된 경우 null.
    /// </summary>
    public int? StoreCode { get; set; }


    /// <summary>
    /// 사용된 라이선스 코드.
    /// </summary>
    public int LicenseCode { get; set; }

    /// <summary>
    /// 인증 성공 여부.
    /// </summary>
    public bool Activated { get; set; }

    /// <summary>
    /// 서버가 발급한 인증 토큰 정보.
    /// 인증 DLL은 이 토큰을 로컬에 저장한다.
    /// </summary>
    public AuthTokenDto Token { get; set; } = new();
}