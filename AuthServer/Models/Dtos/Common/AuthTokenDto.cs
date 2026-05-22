namespace poscam.AuthServer.Models.Dtos.Common;

/// <summary>
/// 인증 성공 후 서버가 클라이언트에 반환하는 토큰 정보 DTO.
/// 
/// PC 캠과 캠뷰어 모두 인증 성공 시 이 구조를 사용한다.
/// 실제 Token 문자열의 생성 방식은 TokenService에서 담당한다.
/// </summary>
public class AuthTokenDto
{
    /// <summary>
    /// 서버가 발급한 인증 토큰 문자열.
    /// 1차 구현에서는 내부 서명 토큰 또는 JWT 방식 중 하나로 구현 가능하다.
    /// </summary>
    public string Token { get; set; } = "";

    /// <summary>
    /// 토큰 발급 시각.
    /// </summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>
    /// 토큰 만료 시각.
    /// 정상 온라인 환경에서 재인증 또는 토큰 갱신 기준이 된다.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// 서버 접속 실패 시 오프라인 실행을 허용하는 만료 시각.
    /// 계약 유형에 따라 다르게 계산한다.
    /// </summary>
    public DateTime OfflineUntil { get; set; }

    /// <summary>
    /// 영구 사용 여부.
    /// 구매형 또는 서비스 종료 Legacy Mode에서 true가 될 수 있다.
    /// </summary>
    public bool IsPermanent { get; set; }

    /// <summary>
    /// 캠뷰어 설정 버전.
    /// PC 캠에서는 사용하지 않을 수 있다.
    /// 캠뷰어는 로컬 설정 파일과 서버 설정 비교에 사용한다.
    /// </summary>
    public string? ConfigVersion { get; set; }
}