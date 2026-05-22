namespace poscam.AuthServer.Models.Dtos.Common;

/// <summary>
/// 인증 토큰 내부에 들어가는 Payload DTO.
/// 
/// 외부 응답용 DTO가 아니라 서버 내부에서 토큰 검증 후 사용하는 데이터 구조다.
/// 토큰 안에 deviceCode가 포함되어야 devices 테이블에서 장비 해제 여부를 확인할 수 있다.
/// </summary>
public class AuthTokenPayloadDto
{
    /// <summary>
    /// 매장 코드.
    /// 
    /// 매장과 연결된 계약으로 인증된 장비는 매장코드가 들어가고,
    /// 매장 없이 생성된 계약으로 인증된 장비는 null이 들어간다.
    /// </summary>
    public int? StoreCode { get; set; }

    /// <summary>
    /// 계약 코드.
    /// </summary>
    public int ContractCode { get; set; }

    /// <summary>
    /// 라이선스 코드.
    /// PC캠은 값이 있고, 캠뷰어는 null일 수 있다.
    /// </summary>
    public int? LicenseCode { get; set; }

    /// <summary>
    /// 장비 코드.
    /// devices.dev_code 값이다.
    /// 토큰이 있어도 이 장비가 devices에 없으면 사용이 해제된 장비로 본다.
    /// </summary>
    public int DeviceCode { get; set; }

    /// <summary>
    /// 앱 유형.
    /// 1 = PC캠, 2 = 캠뷰어
    /// </summary>
    public int AppType { get; set; }

    /// <summary>
    /// 장비 HWID.
    /// 현재 실행 중인 장비의 HWID와 반드시 일치해야 한다.
    /// </summary>
    public string Hwid { get; set; } = "";

    /// <summary>
    /// 토큰 발급 시각.
    /// </summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>
    /// 토큰 만료 시각.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// 오프라인 허용 만료 시각.
    /// 서버 접속 실패 시 클라이언트가 참고한다.
    /// </summary>
    public DateTime OfflineUntil { get; set; }

    /// <summary>
    /// 영구 사용 여부.
    /// 구매형 또는 Legacy Mode에서 사용할 수 있다.
    /// </summary>
    public bool IsPermanent { get; set; }

    /// <summary>
    /// 캠뷰어 설정 버전.
    /// PC캠에서는 null일 수 있다.
    /// </summary>
    public string? ConfigVersion { get; set; }
}