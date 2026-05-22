namespace poscam.AuthServer.Models.Dtos.UserManage;

/// <summary>
/// 관리자 요청 처리 DTO.
/// 
/// 담당자가 등록한 요청을 관리자가 처리할 때 사용한다.
/// 처리 방식:
/// - 승인/처리완료
/// - 반려
/// </summary>
public class UserRequestProcessRequest
{
    /// <summary>
    /// 처리 메모.
    /// users.user_request_result_memo,
    /// userlog.ulog_memo에 기록한다.
    /// </summary>
    public string? Memo { get; set; }
}