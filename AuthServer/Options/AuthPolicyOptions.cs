namespace poscam.AuthServer.Options;

/// <summary>
/// POSCAM 인증 정책 옵션.
/// 
/// appsettings.json의 AuthPolicy 섹션과 매핑된다.
/// 토큰 만료시간, 오프라인 허용기간, 토큰 서명키 등을 관리한다.
/// </summary>
public sealed class AuthPolicyOptions
{
    /// <summary>
    /// 인증 토큰 서명에 사용할 서버 비밀키.
    /// 운영 환경에서는 반드시 충분히 긴 난수 문자열을 사용해야 한다.
    /// </summary>
    public string TokenSecret { get; set; } = "CHANGE_ME_POSCAM_AUTH_SECRET_KEY";

    /// <summary>
    /// 일반 인증 토큰 만료 시간.
    /// 단위: 시간
    /// </summary>
    public int TokenExpireHours { get; set; } = 24;

    /// <summary>
    /// PC캠 정식 인증 후 서버 접속 실패 시 허용할 오프라인 지속 일수.
    /// PC캠 인증 절차 기획서 기준 기본값은 7일이다.
    /// </summary>
    public int PccamOfflineDays { get; set; } = 7;

    /// <summary>
    /// 테스트형 계약의 오프라인 허용일.
    /// </summary>
    public int TrialOfflineDays { get; set; } = 1;

    /// <summary>
    /// 구독형 계약의 오프라인 허용일.
    /// </summary>
    public int SubscriptionOfflineDays { get; set; } = 3;

    /// <summary>
    /// 구매형 계약의 오프라인 허용일.
    /// 
    /// 구매형은 장기 또는 영구 사용 정책을 적용할 수 있으나,
    /// 1차 구현에서는 넉넉한 일수로 토큰을 발급하고,
    /// Legacy Mode에서 isPermanent를 별도로 적용한다.
    /// </summary>
    public int PurchaseOfflineDays { get; set; } = 3650;

    /// <summary>
    /// PC 캠 인증키 접두어.
    /// </summary>
    public string PccamLicensePrefix { get; set; } = "PCM";

    /// <summary>
    /// 매장 ID 최초 시작값.
    /// 
    /// 매장 ID는 영문 2자리 + 숫자 4자리 형식이다.
    /// 예: PC1000
    /// 
    /// AA0001부터 시작하지 않고, 이 설정값부터 시작한다.
    /// </summary>
    public string StoreIdStartValue { get; set; } = "CA0112";

    /// <summary>
    /// 관리자/담당자 로그인 토큰 만료 시간.
    /// 단위: 시간
    /// </summary>
    public int AccountTokenExpireHours { get; set; } = 24;

    //public const string SectionName = "AuthPolicy";

    //public string TokenIssuer { get; set; } = "poscam.AuthServer";
    //public string TokenAudience { get; set; } = "poscam.Clients";
    //public string TokenSecret { get; set; } = "CHANGE_THIS_TO_A_LONG_RANDOM_SECRET_KEY_AT_LEAST_32_CHARS";

    ///// <summary>
    ///// 일반 인증 토큰 만료 시간. 1차 구현에서는 하루 단위 갱신을 기본값으로 둔다.
    ///// </summary>
    //public int AccessTokenMinutes { get; set; } = 1440;

    //public int TrialOfflineDays { get; set; } = 3;
    //public int SubscriptionOfflineDays { get; set; } = 7;
    //public int PurchaseOfflineDays { get; set; } = 3650;

    ///// <summary>
    ///// 서비스 종료 또는 구매형 고객을 위한 영구 모드 전환 여부.
    ///// 1차 구현에서는 기본 false로 두고, 후속 단계에서 관리자 API로 제어한다.
    ///// </summary>
    //public bool EnableLegacyMode { get; set; } = false;

    //public string PccamLicensePrefix { get; set; } = "PCM";
    //public string ViewerLicensePrefix { get; set; } = "CVW";
    //public int LicenseKeyGroupLength { get; set; } = 4;
    //public int LicenseKeyGroupCount { get; set; } = 3;
    //public string LicenseKeyAllowedChars { get; set; } = "23456789ABCDEFGHJKMNPRSTUVWXYZ";
}
