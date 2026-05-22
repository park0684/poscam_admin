namespace poscam.AuthServer.Models.Enums;

/// <summary>
/// 사용자 계정 상태.
/// users.user_status 값과 매칭된다.
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// 가입 후 관리자 승인 대기 상태.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 정상 사용 가능 상태.
    /// </summary>
    Active = 1,

    /// <summary>
    /// 일시중지 상태.
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// 무효.
    /// 퇴사, 담당자 변경, 중복 계정 등으로 더 이상 사용하지 않는 상태.
    /// 로그인 불가.
    /// </summary>
    Invalid = 3,

    /// <summary>
    /// 차단 상태.
    /// </summary>
    Blocked = 9
}