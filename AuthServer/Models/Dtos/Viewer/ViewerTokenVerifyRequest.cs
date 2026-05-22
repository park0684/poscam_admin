namespace poscam.AuthServer.Models.Dtos.Viewer;

/// <summary>
/// 캠뷰어 토큰 실행 인증 요청 DTO.
/// 
/// 캠뷰어는 최초 로그인 이후에는 ID/비밀번호를 다시 입력하지 않고,
/// 로컬에 저장된 토큰으로 실행 인증을 요청한다.
/// </summary>
public class ViewerTokenVerifyRequest
{
    /// <summary>
    /// 로컬에 저장된 인증 토큰.
    /// </summary>
    public string Token { get; set; } = "";

    /// <summary>
    /// 현재 장비의 HWID.
    /// 토큰에 저장된 HWID와 일치해야 한다.
    /// </summary>
    public string Hwid { get; set; } = "";

    /// <summary>
    /// 캠뷰어 프로그램 버전.
    /// </summary>
    public string? ProgramVersion { get; set; }
}