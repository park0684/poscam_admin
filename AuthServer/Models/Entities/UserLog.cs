namespace poscam.AuthServer.Models.Entities;

/// <summary>
/// 사용자 계정 로그 Entity.
/// 
/// userlog 테이블과 매핑된다.
/// 담당자 등록, 승인, 상태 변경 요청, 처리 결과, 정보수정, 비밀번호 변경 등의 이력을 기록한다.
/// </summary>
public class UserLog
{
    /// <summary>
    /// 로그 코드.
    /// DB 컬럼: ulog_code
    /// </summary>
    public int UlogCode { get; set; }

    /// <summary>
    /// 로그 대상 사용자 코드.
    /// DB 컬럼: user_code
    /// </summary>
    public int UserCode { get; set; }

    /// <summary>
    /// 로그 대상 사용자의 파트너사 코드.
    /// DB 컬럼: partner_code
    /// </summary>
    public int? PartnerCode { get; set; }

    /// <summary>
    /// 로그 유형.
    /// DB 컬럼: ulog_type
    /// UserLogType enum 값 사용.
    /// </summary>
    public int UlogType { get; set; }

    /// <summary>
    /// 요청 유형.
    /// DB 컬럼: ulog_request_type
    /// UserRequestType enum 값 사용.
    /// </summary>
    public int? UlogRequestType { get; set; }

    /// <summary>
    /// 요청 상태.
    /// DB 컬럼: ulog_request_status
    /// UserRequestStatus enum 값 사용.
    /// </summary>
    public int? UlogRequestStatus { get; set; }

    /// <summary>
    /// 변경 전 사용자 상태.
    /// DB 컬럼: ulog_before_status
    /// </summary>
    public int? UlogBeforeStatus { get; set; }

    /// <summary>
    /// 변경 후 사용자 상태.
    /// DB 컬럼: ulog_after_status
    /// </summary>
    public int? UlogAfterStatus { get; set; }

    /// <summary>
    /// 요청 또는 변경 사유.
    /// DB 컬럼: ulog_reason
    /// </summary>
    public string? UlogReason { get; set; }

    /// <summary>
    /// 처리 메모.
    /// DB 컬럼: ulog_memo
    /// </summary>
    public string? UlogMemo { get; set; }

    /// <summary>
    /// 변경 필드 JSON 문자열.
    /// DB 컬럼: ulog_changed_fields
    /// 
    /// 예:
    /// {
    ///   "userName": { "before": "홍길동", "after": "김길동" },
    ///   "userCell": { "before": "010-1111-2222", "after": "010-3333-4444" }
    /// }
    /// </summary>
    public string? UlogChangedFields { get; set; }

    /// <summary>
    /// 요청자 user_code.
    /// DB 컬럼: ulog_requested_by
    /// </summary>
    public int? UlogRequestedBy { get; set; }

    /// <summary>
    /// 처리자 user_code.
    /// DB 컬럼: ulog_processed_by
    /// </summary>
    public int? UlogProcessedBy { get; set; }

    /// <summary>
    /// 요청일.
    /// DB 컬럼: ulog_requested_at
    /// </summary>
    public DateTime? UlogRequestedAt { get; set; }

    /// <summary>
    /// 처리일.
    /// DB 컬럼: ulog_processed_at
    /// </summary>
    public DateTime? UlogProcessedAt { get; set; }

    /// <summary>
    /// 로그 등록일.
    /// DB 컬럼: ulog_rdate
    /// </summary>
    public DateTime UlogRdate { get; set; }
}