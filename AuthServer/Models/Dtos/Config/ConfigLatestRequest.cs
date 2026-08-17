namespace poscam.AuthServer.Models.Dtos.Config;

/// <summary>
/// 캠뷰어 최신 설정 조회 요청 DTO.
///
/// 캠뷰어는 로컬에 저장된 토큰과 현재 HWID를 서버에 전달하여
/// 설정 다운로드 권한을 확인받는다.
/// </summary>
public class ConfigLatestRequest
{
    /// <summary>
    /// 캠뷰어 로그인 또는 토큰 검증을 통해 발급받은 토큰.
    /// </summary>
    public string Token { get; set; } = "";

    /// <summary>
    /// 현재 캠뷰어 장비의 HWID.
    /// 토큰에 포함된 HWID와 일치해야 한다.
    /// </summary>
    public string Hwid { get; set; } = "";

    /// <summary>
    /// 클라이언트가 이해하는 설정 스키마 버전.
    /// 값이 없거나 2 미만이면 기존 단일 NVR 클라이언트로 본다.
    /// </summary>
    public int ConfigSchemaVersion { get; set; }

    /// <summary>
    /// 캠뷰어 로컬 설정 버전.
    /// 서버 버전과 비교할 때 사용한다.
    /// </summary>
    public string? LocalConfigVersion { get; set; }

    /// <summary>
    /// 캠뷰어 프로그램 버전.
    /// 로그 기록용.
    /// </summary>
    public string? ProgramVersion { get; set; }
}
