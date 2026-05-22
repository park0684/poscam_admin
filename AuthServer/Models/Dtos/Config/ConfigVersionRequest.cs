namespace poscam.AuthServer.Models.Dtos.Config;

/// <summary>
/// 캠뷰어 설정 버전 조회 요청 DTO.
/// 
/// 전체 설정을 내려받기 전에 서버 설정 버전만 확인할 때 사용한다.
/// </summary>
public class ConfigVersionRequest
{
    public string Token { get; set; } = "";

    public string Hwid { get; set; } = "";

    public string? LocalConfigVersion { get; set; }

    public string? ProgramVersion { get; set; }
}