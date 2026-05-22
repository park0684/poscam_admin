namespace poscam.AuthServer.Models.Enums;

/// <summary>
/// 파트너사 상태.
/// partners.partner_status 값과 매칭된다.
/// </summary>
public enum PartnerStatus
{
    /// <summary>
    /// 비활성 상태.
    /// </summary>
    Inactive = 0,

    /// <summary>
    /// 정상 상태.
    /// </summary>
    Active = 1,

    /// <summary>
    /// 일시중지 상태.
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// 차단 상태.
    /// </summary>
    Blocked = 9
}