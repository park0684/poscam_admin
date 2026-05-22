namespace poscam.AuthServer.Models.Dtos.Account;

/// <summary>
/// 사용자/담당자 목록 DTO.
/// 
/// 직원 관리 화면, 담당자 배정 화면, 파트너사 직원 목록 화면에서 사용한다.
/// user_code는 화면에 직접 표시하지 않지만,
/// 상세 이동과 내부 처리에는 필요하므로 DTO에는 포함한다.
/// </summary>
public class UserListItemDto
{
    /// <summary>
    /// 사용자 코드.
    /// 화면에는 표시하지 않지만 내부적으로 상세 조회, 상태 변경 등에 사용한다.
    /// </summary>
    public int UserCode { get; set; }

    /// <summary>
    /// 소속 파트너사 코드.
    /// 관리자는 null 가능.
    /// </summary>
    public int? PartnerCode { get; set; }

    /// <summary>
    /// 파트너사명.
    /// 목록에서 사용자가 어느 파트너사 소속인지 보여주기 위해 사용한다.
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
    /// 담당자 연락처.
    /// </summary>
    public string? UserCell { get; set; }

    /// <summary>
    /// 담당자 이메일.
    /// </summary>
    public string? UserEmail { get; set; }

    /// <summary>
    /// 사용자 역할.
    /// 1=관리자, 2=담당자.
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
    /// 최근 요청자 user_code.
    /// </summary>
    public int? UserRequestedBy { get; set; }

    /// <summary>
    /// 최근 요청일.
    /// </summary>
    public DateTime? UserRequestedAt { get; set; }

    /// <summary>
    /// 최근 요청 처리 결과 메모.
    /// </summary>
    public string? UserRequestResultMemo { get; set; }
}