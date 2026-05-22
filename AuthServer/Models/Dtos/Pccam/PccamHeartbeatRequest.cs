namespace poscam.AuthServer.Models.Dtos.Pccam;

/// <summary>
/// PC캠 하트비트 요청 DTO.
/// 
/// 인증 판단의 핵심 API는 아니며,
/// 현재는 실행 중 장비의 생존 기록을 auth_logs에 남기기 위한 용도로만 사용한다.
/// </summary>
public class PccamHeartbeatRequest
{
    /// <summary>
    /// 현재 PC의 장비 식별값.
    /// </summary>
    public string Hwid { get; set; } = "";
}