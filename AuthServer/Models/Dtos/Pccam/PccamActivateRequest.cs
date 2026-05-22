namespace poscam.AuthServer.Models.Dtos.Pccam;

/// <summary>
/// PC캠 최초 인증 요청 DTO.
/// 
/// 사용자는 인증키만 입력하고,
/// 서버는 인증키에 연결된 계약정보를 기준으로 매장을 자동 판별한다.
/// </summary>
public class PccamActivateRequest
{
    /// <summary>
    /// 사용자가 입력한 PC캠 인증키.
    /// 예: PCM-A8K2-7M9P-W3X4
    /// </summary>
    public string LicenseKey { get; set; } = "";

    /// <summary>
    /// 현재 PC의 HWID.
    /// 최초 인증 성공 시 이 HWID에 인증키가 귀속된다.
    /// </summary>
    public string Hwid { get; set; } = "";

    /// <summary>
    /// 장비명.
    /// 예: 계산대 PC, 매장 메인 PC
    /// </summary>
    public string? DeviceName { get; set; }
}