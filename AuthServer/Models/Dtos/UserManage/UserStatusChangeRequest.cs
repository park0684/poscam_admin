namespace poscam.AuthServer.Models.Dtos.UserManage;

/// <summary>
/// 사용자 상태 변경 요청 DTO.
/// 
/// 실제 상태 변경은 관리자만 가능하다.
/// 
/// 상태 변경 예:
/// - 일시중지
/// - 정상복구
/// - 무효
/// - 차단
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
    /// userlog.ulog_memo에 기록한다.
    /// </summary>
    public string? Memo { get; set; }
}