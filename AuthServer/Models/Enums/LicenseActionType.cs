namespace poscam.AuthServer.Models.Enums;

/// <summary>
/// 라이선스 로그 작업 유형.
/// </summary>
public enum LicenseActionType
{
    /// <summary>
    /// 라이선스 발급.
    /// </summary>
    Issue = 1,

    /// <summary>
    /// 라이선스 활성화.
    /// </summary>
    Activate = 2,

    /// <summary>
    /// 라이선스 초기화.
    /// </summary>
    Reset = 3,

    /// <summary>
    /// 라이선스 폐기.
    /// </summary>
    Revoke = 4,

    /// <summary>
    /// 인증 검증.
    /// </summary>
    Verify = 5,

    /// <summary>
    /// 하트비트.
    /// </summary>
    Heartbeat = 6,

    /// <summary>
    /// 장비 해제.
    /// </summary>
    DeviceRelease = 7,

    /// <summary>
    /// 기존 사용 허용 처리.
    /// </summary>
    LegacyEnable = 8,

    /// <summary>
    /// 폐기된 라이선스 복구.
    /// </summary>
    Restore = 9
}