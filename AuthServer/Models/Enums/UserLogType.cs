namespace poscam.AuthServer.Models.Enums;

/// <summary>
/// 사용자 계정 변경 및 요청 로그 유형.
/// 
/// userlog.ulog_type 컬럼과 매핑된다.
/// </summary>
public enum UserLogType
{
    /// <summary>
    /// 담당자 계정 등록.
    /// </summary>
    Register = 1,

    /// <summary>
    /// 가입 승인 요청.
    /// </summary>
    ApprovalRequest = 2,

    /// <summary>
    /// 가입 승인 처리.
    /// </summary>
    ApprovalCompleted = 3,

    /// <summary>
    /// 가입 승인 반려.
    /// </summary>
    ApprovalRejected = 4,

    /// <summary>
    /// 정보 수정 요청.
    /// </summary>
    InfoChangeRequest = 5,

    /// <summary>
    /// 정보 수정 처리.
    /// </summary>
    InfoChangeCompleted = 6,

    /// <summary>
    /// 비밀번호 변경.
    /// 담당자 본인 또는 관리자가 비밀번호를 변경한 경우.
    /// </summary>
    PasswordChanged = 7,

    /// <summary>
    /// 비밀번호 초기화.
    /// 관리자가 담당자 비밀번호를 초기화한 경우.
    /// </summary>
    PasswordReset = 8,

    /// <summary>
    /// 일시중지 요청.
    /// </summary>
    SuspendRequest = 9,

    /// <summary>
    /// 일시중지 처리.
    /// </summary>
    SuspendCompleted = 10,

    /// <summary>
    /// 정상복구 요청.
    /// </summary>
    RestoreRequest = 11,

    /// <summary>
    /// 정상복구 처리.
    /// </summary>
    RestoreCompleted = 12,

    /// <summary>
    /// 무효 요청.
    /// 퇴사, 담당자 변경, 중복 계정 등의 사유.
    /// </summary>
    InvalidRequest = 13,

    /// <summary>
    /// 무효 처리.
    /// </summary>
    InvalidCompleted = 14,

    /// <summary>
    /// 차단 요청.
    /// </summary>
    BlockRequest = 15,

    /// <summary>
    /// 차단 처리.
    /// </summary>
    BlockCompleted = 16
}