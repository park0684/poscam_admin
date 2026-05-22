namespace poscam.AuthServer.Models.Dtos.Pccam;

/// <summary>
/// PC 캠 하트비트 응답 DTO.
/// </summary>
public class PccamHeartbeatResponse
{
    /// <summary>
    /// 현재 장비의 인증 유효 여부.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 서버 현재 시간.
    /// 클라이언트 시간 조작 감지에 참고할 수 있다.
    /// </summary>
    public DateTime ServerTime { get; set; }
}