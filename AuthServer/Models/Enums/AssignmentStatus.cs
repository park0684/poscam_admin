namespace poscam.AuthServer.Models.Enums;

/// <summary>
/// 매장 담당자 배정 상태.
/// store_user_assignments.status 값과 매칭된다.
/// </summary>
public enum AssignmentStatus
{
    /// <summary>
    /// 비활성 상태.
    /// </summary>
    Inactive = 0,

    /// <summary>
    /// 정상 배정 상태.
    /// </summary>
    Active = 1,

    /// <summary>
    /// 해제 상태.
    /// </summary>
    Released = 9
}