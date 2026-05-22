namespace poscam.AuthServer.Models.Dtos.UserManage;

/// <summary>
/// 담당자가 상태 변경 또는 정보 변경을 요청할 때 사용하는 DTO.
/// 
/// 담당자는 직접 사용자 상태를 변경하지 않고 요청만 등록한다.
/// 관리자가 요청을 검토한 뒤 실제 상태 변경 또는 정보 수정을 처리한다.
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
    /// 정보수정 요청 시 변경 희망 내용을 JSON 문자열로 저장한다.
    /// 
    /// 예:
    /// {
    ///   "userCell": "010-1111-2222",
    ///   "userEmail": "test@example.com"
    /// }
    /// </summary>
    public string? RequestedChangeJson { get; set; }
}