namespace poscam.AdminWeb.Models.UserManage;

/// <summary>
/// 직원/담당자 목록 조회 DTO.
/// 
/// AuthServer의 GET /api/manage/users 응답 Data 항목과 구조를 맞춘다.
/// userCode는 화면에 직접 표시하지 않지만,
/// 상세 페이지 이동과 내부 처리에 필요하므로 포함한다.
/// </summary>
public class UserManageListItemDto
{
    /// <summary>
    /// 사용자 코드.
    /// 화면에는 표시하지 않는다.
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
    /// 담당자 이름.
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
    /// 승인한 관리자 코드.
    /// </summary>
    public int? ApprovedBy { get; set; }

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
    /// 1=가입승인, 2=정보수정, 3=비밀번호초기화,
    /// 4=일시중지, 5=정상복구, 6=무효, 9=차단.
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
    /// 요청일.
    /// </summary>
    public DateTime? UserRequestedAt { get; set; }

    /// <summary>
    /// 요청 처리 결과 메모.
    /// </summary>
    public string? UserRequestResultMemo { get; set; }
}