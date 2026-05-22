namespace poscam.AuthServer.Models.Enums;

/// <summary>
/// 사용자 계정 관련 요청 유형.
/// 
/// users.user_request_type,
/// userlog.ulog_request_type 컬럼과 매핑된다.
/// </summary>
public enum UserRequestType
{
    /// <summary>
    /// 가입 승인 요청.
    /// 신규 담당자 등록 후 관리자 승인을 기다리는 요청.
    /// </summary>
    JoinApproval = 1,

    /// <summary>
    /// 정보 수정 요청.
    /// 담당자가 본인 또는 파트너사 직원 정보 변경을 요청하는 경우.
    /// </summary>
    InfoChange = 2,

    /// <summary>
    /// 비밀번호 초기화 요청.
    /// 담당자가 비밀번호 초기화를 요청하는 경우.
    /// </summary>
    PasswordReset = 3,

    /// <summary>
    /// 일시중지 요청.
    /// 계정 사용을 임시로 중단해달라는 요청.
    /// </summary>
    Suspend = 4,

    /// <summary>
    /// 정상복구 요청.
    /// 일시중지 또는 차단 상태에서 정상 상태로 복구 요청.
    /// </summary>
    Restore = 5,

    /// <summary>
    /// 무효 요청.
    /// 퇴사, 담당자 변경, 중복 등록 등으로 계정을 무효 처리해달라는 요청.
    /// </summary>
    Invalid = 6,

    /// <summary>
    /// 차단 요청.
    /// 보안상 문제 등으로 계정 차단을 요청하는 경우.
    /// </summary>
    Block = 9
}