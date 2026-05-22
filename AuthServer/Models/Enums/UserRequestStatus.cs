namespace poscam.AuthServer.Models.Enums;

/// <summary>
/// 사용자 계정 관련 요청 처리 상태.
/// 
/// users.user_request_status,
/// userlog.ulog_request_status 컬럼과 매핑된다.
/// </summary>
public enum UserRequestStatus
{
    /// <summary>
    /// 요청 없음.
    /// </summary>
    None = 0,

    /// <summary>
    /// 요청 대기.
    /// 담당자 또는 시스템이 요청을 등록했고 아직 처리되지 않은 상태.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// 처리 완료.
    /// 관리자가 요청을 처리한 상태.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// 반려.
    /// 관리자가 요청을 거절한 상태.
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// 취소.
    /// 요청자가 요청을 취소한 상태.
    /// </summary>
    Canceled = 9
}