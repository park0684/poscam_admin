namespace poscam.AuthServer.Models.Entities;

/// <summary>
/// 사용자 계정 Entity.
/// DB 테이블: users
/// 
/// 관리자와 파트너 담당자를 같은 테이블에서 관리한다.
/// user_role 값으로 관리자와 담당자를 구분한다.
/// </summary>
public class UserAccount
{
    /// <summary>
    /// 사용자 고유 코드.
    /// DB 컬럼: user_code
    /// </summary>
    public int UserCode { get; set; }

    /// <summary>
    /// 소속 파트너사 코드.
    /// 내부 관리자는 null일 수 있다.
    /// DB 컬럼: partner_code
    /// </summary>
    public int? PartnerCode { get; set; }

    /// <summary>
    /// 로그인 ID.
    /// DB 컬럼: user_id
    /// </summary>
    public string UserId { get; set; } = "";

    /// <summary>
    /// 비밀번호 해시.
    /// DB 컬럼: user_password_hash
    /// </summary>
    public string UserPasswordHash { get; set; } = "";

    /// <summary>
    /// 담당자명.
    /// DB 컬럼: user_name
    /// </summary>
    public string UserName { get; set; } = "";

    /// <summary>
    /// 담당자 연락처.
    /// DB 컬럼: user_cell
    /// </summary>
    public string? UserCell { get; set; }

    /// <summary>
    /// 담당자 이메일.
    /// DB 컬럼: user_email
    /// </summary>
    public string? UserEmail { get; set; }

    /// <summary>
    /// 사용자 권한.
    /// UserRole enum 값과 매칭된다.
    /// DB 컬럼: user_role
    /// </summary>
    public int UserRole { get; set; }

    /// <summary>
    /// 사용자 상태.
    /// UserStatus enum 값과 매칭된다.
    /// DB 컬럼: user_status
    /// </summary>
    public int UserStatus { get; set; }

    /// <summary>
    /// 승인한 관리자 user_code.
    /// DB 컬럼: approved_by
    /// </summary>
    public int? ApprovedBy { get; set; }

    /// <summary>
    /// 승인일시.
    /// DB 컬럼: approved_at
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// 가입일.
    /// DB 컬럼: user_rdate
    /// </summary>
    public DateTime UserRDate { get; set; }

    /// <summary>
    /// 수정일.
    /// DB 컬럼: user_udate
    /// </summary>
    public DateTime? UserUDate { get; set; }

    /// <summary>
    /// 최근 요청 유형.
    /// 
    /// DB 컬럼: users.user_request_type
    /// 예: 가입승인, 정보수정, 비밀번호초기화, 일시중지, 정상복구, 무효, 차단.
    /// </summary>
    public int? UserRequestType { get; set; }

    /// <summary>
    /// 최근 요청 상태.
    /// 
    /// DB 컬럼: users.user_request_status
    /// 0=요청없음, 1=요청대기, 2=처리완료, 3=반려, 9=취소.
    /// </summary>
    public int UserRequestStatus { get; set; }

    /// <summary>
    /// 최근 요청 사유.
    /// 
    /// DB 컬럼: users.user_request_reason
    /// </summary>
    public string? UserRequestReason { get; set; }

    /// <summary>
    /// 요청자 user_code.
    /// 
    /// DB 컬럼: users.user_requested_by
    /// 담당자가 요청한 경우 해당 담당자 user_code가 들어간다.
    /// </summary>
    public int? UserRequestedBy { get; set; }

    /// <summary>
    /// 요청일.
    /// 
    /// DB 컬럼: users.user_requested_at
    /// </summary>
    public DateTime? UserRequestedAt { get; set; }

    /// <summary>
    /// 요청 처리 결과 메모.
    /// 
    /// DB 컬럼: users.user_request_result_memo
    /// 관리자가 승인/반려/처리 시 남기는 메모.
    /// </summary>
    public string? UserRequestResultMemo { get; set; }

    /// <summary>
    /// 소속 파트너사명.
    /// 상세 화면 표시용으로 사용한다.
    /// </summary>
    public string? PartnerName { get; set; }
}