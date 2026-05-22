namespace poscam.AdminWeb.Models.UserManage;

/// <summary>
/// 직원/담당자 상세 조회 DTO.
///
/// AuthServer의 GET /api/manage/users/{userCode} 응답 Data와 구조를 맞춘다.
/// 비밀번호 해시는 절대 포함하지 않는다.
/// </summary>
public class UserManageDetailDto
{
    /// <summary>
    /// 사용자 코드.
    /// 화면에는 직접 표시하지 않지만 상세 조회, 수정, 승인, 상태 변경에 사용한다.
    /// </summary>
    public int UserCode { get; set; }

    /// <summary>
    /// 소속 파트너사 코드.
    /// </summary>
    public int? PartnerCode { get; set; }

    /// <summary>
    /// 소속 파트너사명.
    /// </summary>
    public string? PartnerName { get; set; }

    /// <summary>
    /// 로그인 ID.
    /// </summary>
    public string UserId { get; set; } = "";

    /// <summary>
    /// 담당자명.
    /// </summary>
    public string UserName { get; set; } = "";

    /// <summary>
    /// 연락처.
    /// </summary>
    public string? UserCell { get; set; }

    /// <summary>
    /// 이메일.
    /// </summary>
    public string? UserEmail { get; set; }

    /// <summary>
    /// 사용자 역할.
    /// 1=관리자, 2=파트너 담당자.
    /// </summary>
    public int UserRole { get; set; }

    /// <summary>
    /// 사용자 상태.
    /// 0=승인대기, 1=정상, 2=일시중지, 3=무효, 9=차단.
    /// </summary>
    public int UserStatus { get; set; }

    /// <summary>
    /// 승인한 관리자 user_code.
    /// </summary>
    public int? ApprovedBy { get; set; }

    /// <summary>
    /// 승인한 관리자 이름.
    /// 백엔드에서 제공하지 않으면 null일 수 있다.
    /// </summary>
    public string? ApprovedByName { get; set; }

    /// <summary>
    /// 승인일.
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// 등록일.
    /// </summary>
    public DateTime UserRdate { get; set; }

    /// <summary>
    /// 수정일.
    /// </summary>
    public DateTime? UserUdate { get; set; }

    /// <summary>
    /// 최근 요청 유형.
    /// 1=가입승인, 2=정보수정, 3=비밀번호초기화, 4=일시중지, 5=정상복구, 6=무효, 9=차단.
    /// </summary>
    public int? UserRequestType { get; set; }

    /// <summary>
    /// 최근 요청 상태.
    /// 0=요청없음, 1=요청대기, 2=처리완료, 3=반려, 9=취소.
    /// </summary>
    public int UserRequestStatus { get; set; }

    /// <summary>
    /// 최근 요청 사유.
    /// </summary>
    public string? UserRequestReason { get; set; }

    /// <summary>
    /// 요청자 user_code.
    /// </summary>
    public int? UserRequestedBy { get; set; }

    /// <summary>
    /// 요청자 이름.
    /// 백엔드에서 제공하지 않으면 null일 수 있다.
    /// </summary>
    public string? UserRequestedByName { get; set; }

    /// <summary>
    /// 요청일.
    /// </summary>
    public DateTime? UserRequestedAt { get; set; }

    /// <summary>
    /// 요청 처리 결과 메모.
    /// </summary>
    public string? UserRequestResultMemo { get; set; }

    /// <summary>
    /// 현재 로그인 사용자가 정보 수정 가능한지 여부.
    /// 현재 정책상 관리자는 true, 담당자는 false.
    /// </summary>
    public bool CanEdit { get; set; }

    /// <summary>
    /// 현재 로그인 사용자가 변경 요청을 등록할 수 있는지 여부.
    /// 담당자는 본인 파트너사 직원에 대해 요청 가능.
    /// </summary>
    public bool CanRequestChange { get; set; }

    /// <summary>
    /// 현재 로그인 사용자가 상태를 직접 변경할 수 있는지 여부.
    /// 현재 정책상 관리자만 true.
    /// </summary>
    public bool CanChangeStatus { get; set; }
}

/// <summary>
/// 직원/담당자 신규 등록 요청 DTO.
///
/// 관리자:
/// - PartnerCode를 선택해서 등록한다.
///
/// 담당자:
/// - 서버에서 로그인 사용자의 PartnerCode로 강제된다.
/// </summary>
public class UserCreateRequest
{
    /// <summary>
    /// 소속 파트너사 코드.
    /// 관리자 등록 시 사용한다.
    /// 담당자 등록 시에는 서버에서 로그인 사용자의 PartnerCode를 사용한다.
    /// </summary>
    public int? PartnerCode { get; set; }

    /// <summary>
    /// 로그인 ID.
    /// </summary>
    public string UserId { get; set; } = "";

    /// <summary>
    /// 초기 비밀번호.
    /// 서버에서 해시 처리 후 users.user_password_hash에 저장한다.
    /// </summary>
    public string Password { get; set; } = "";

    /// <summary>
    /// 담당자명.
    /// </summary>
    public string UserName { get; set; } = "";

    /// <summary>
    /// 연락처.
    /// </summary>
    public string? UserCell { get; set; }

    /// <summary>
    /// 이메일.
    /// </summary>
    public string? UserEmail { get; set; }

    /// <summary>
    /// 등록 요청 사유.
    /// </summary>
    public string? RequestReason { get; set; }
}

/// <summary>
/// 직원/담당자 정보 수정 요청 DTO.
///
/// 현재 정책상 실제 정보 수정은 관리자만 수행한다.
/// 담당자는 직접 수정하지 않고 변경 요청을 등록한다.
/// </summary>
public class UserUpdateRequest
{
    /// <summary>
    /// 수정 대상 사용자 코드.
    /// Route의 userCode와 일치해야 한다.
    /// </summary>
    public int UserCode { get; set; }

    /// <summary>
    /// 소속 파트너사 코드.
    /// 관리자만 변경 가능하다.
    /// </summary>
    public int? PartnerCode { get; set; }

    /// <summary>
    /// 담당자명.
    /// </summary>
    public string UserName { get; set; } = "";

    /// <summary>
    /// 연락처.
    /// </summary>
    public string? UserCell { get; set; }

    /// <summary>
    /// 이메일.
    /// </summary>
    public string? UserEmail { get; set; }
}

/// <summary>
/// 직원/담당자 등록/수정 응답 DTO.
/// </summary>
public class UserSaveResponse
{
    /// <summary>
    /// 저장된 사용자 코드.
    /// 화면에는 표시하지 않지만 상세 페이지 이동에 사용한다.
    /// </summary>
    public int UserCode { get; set; }

    /// <summary>
    /// 소속 파트너사 코드.
    /// </summary>
    public int? PartnerCode { get; set; }

    /// <summary>
    /// 로그인 ID.
    /// </summary>
    public string UserId { get; set; } = "";

    /// <summary>
    /// 담당자명.
    /// </summary>
    public string UserName { get; set; } = "";

    /// <summary>
    /// 신규 생성 여부.
    /// </summary>
    public bool Created { get; set; }

    /// <summary>
    /// 저장 성공 여부.
    /// </summary>
    public bool Saved { get; set; }
}

/// <summary>
/// 관리자 상태 변경 요청 DTO.
///
/// 실제 상태 변경은 관리자만 가능하다.
/// </summary>
public class UserStatusChangeRequest
{
    /// <summary>
    /// 변경할 상태.
    /// 1=정상, 2=일시중지, 3=무효, 9=차단.
    /// </summary>
    public int NewStatus { get; set; }

    /// <summary>
    /// 처리 메모.
    /// </summary>
    public string? Memo { get; set; }
}

/// <summary>
/// 담당자 변경 요청 등록 DTO.
///
/// 담당자는 실제 상태를 직접 변경하지 않고 요청만 등록한다.
/// </summary>
public class UserRequestCreateRequest
{
    /// <summary>
    /// 요청 대상 사용자 코드.
    /// </summary>
    public int UserCode { get; set; }

    /// <summary>
    /// 요청 유형.
    /// 2=정보수정, 3=비밀번호초기화, 4=일시중지, 5=정상복구, 6=무효, 9=차단.
    /// </summary>
    public int RequestType { get; set; }

    /// <summary>
    /// 요청 사유.
    /// </summary>
    public string? RequestReason { get; set; }

    /// <summary>
    /// 정보수정 요청 시 변경 희망 내용을 JSON 문자열로 보관한다.
    /// 현재 1차 화면에서는 사용하지 않아도 된다.
    /// </summary>
    public string? RequestedChangeJson { get; set; }
}

/// <summary>
/// 관리자 요청 처리 DTO.
///
/// 가입 승인, 요청 반려 등에 사용한다.
/// </summary>
public class UserRequestProcessRequest
{
    /// <summary>
    /// 처리 메모.
    /// </summary>
    public string? Memo { get; set; }
}

/// <summary>
/// 사용자 로그 목록 DTO.
///
/// AuthServer의 GET /api/manage/users/{userCode}/logs 응답 Data 항목과 구조를 맞춘다.
/// </summary>
public class UserLogItemDto
{
    /// <summary>
    /// 로그 코드.
    /// </summary>
    public int UlogCode { get; set; }

    /// <summary>
    /// 로그 대상 사용자 코드.
    /// </summary>
    public int UserCode { get; set; }

    /// <summary>
    /// 대상 사용자의 파트너사 코드.
    /// </summary>
    public int? PartnerCode { get; set; }

    /// <summary>
    /// 로그 유형.
    /// </summary>
    public int UlogType { get; set; }

    /// <summary>
    /// 요청 유형.
    /// </summary>
    public int? UlogRequestType { get; set; }

    /// <summary>
    /// 요청 상태.
    /// </summary>
    public int? UlogRequestStatus { get; set; }

    /// <summary>
    /// 변경 전 사용자 상태.
    /// </summary>
    public int? UlogBeforeStatus { get; set; }

    /// <summary>
    /// 변경 후 사용자 상태.
    /// </summary>
    public int? UlogAfterStatus { get; set; }

    /// <summary>
    /// 요청 사유.
    /// </summary>
    public string? UlogReason { get; set; }

    /// <summary>
    /// 처리 메모.
    /// </summary>
    public string? UlogMemo { get; set; }

    /// <summary>
    /// 변경 필드 JSON 문자열.
    /// </summary>
    public string? UlogChangedFields { get; set; }

    /// <summary>
    /// 요청자 user_code.
    /// </summary>
    public int? UlogRequestedBy { get; set; }

    /// <summary>
    /// 요청자 이름.
    /// 백엔드에서 제공하지 않으면 null일 수 있다.
    /// </summary>
    public string? UlogRequestedByName { get; set; }

    /// <summary>
    /// 처리자 user_code.
    /// </summary>
    public int? UlogProcessedBy { get; set; }

    /// <summary>
    /// 처리자 이름.
    /// 백엔드에서 제공하지 않으면 null일 수 있다.
    /// </summary>
    public string? UlogProcessedByName { get; set; }

    /// <summary>
    /// 요청일.
    /// </summary>
    public DateTime? UlogRequestedAt { get; set; }

    /// <summary>
    /// 처리일.
    /// </summary>
    public DateTime? UlogProcessedAt { get; set; }

    /// <summary>
    /// 로그 등록일.
    /// </summary>
    public DateTime UlogRdate { get; set; }
}
