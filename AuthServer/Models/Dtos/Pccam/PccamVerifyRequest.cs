namespace poscam.AuthServer.Models.Dtos.Pccam;

/// <summary>
/// PC캠 실행 인증 요청 DTO.
/// 
/// 최초 인증 이후에는 인증키가 아니라
/// 로컬에 저장된 인증 토큰을 이용해 실행 인증을 수행한다.
/// </summary>
public class PccamVerifyRequest
{
    /// <summary>
    /// 인증 DLL이 로컬에 저장한 서버 발급 토큰.
    /// </summary>
    public string Token { get; set; } = "";

    /// <summary>
    /// 현재 PC의 장비 식별값.
    /// 토큰 내부 HWID와 반드시 일치해야 한다.
    /// </summary>
    public string Hwid { get; set; } = "";
}