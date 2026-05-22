namespace poscam.AuthServer.Models.Dtos.UserManage;

/// <summary>
/// 사용자 비밀번호 변경/초기화 요청 DTO.
/// 
/// 관리자 또는 권한 보유 계정은 기존 비밀번호 확인 없이 새 비밀번호로 초기화할 수 있다.
/// 사용자가 본인 비밀번호를 직접 변경할 경우 CurrentPassword를 사용한다.
/// </summary>
public class UserPasswordChangeRequest
{
    /// <summary>
    /// 담당자 본인이 직접 변경할 경우 현재 비밀번호 확인에 사용.
    /// 관리자 초기화 시에는 사용하지 않는다.
    /// </summary>
    public string? CurrentPassword { get; set; }

    /// <summary>
    /// 새 비밀번호.
    /// 서버에서 해시 처리 후 users.user_password_hash에 저장한다.
    /// </summary>
    public string NewPassword { get; set; } = "";

    /// <summary>
    /// 처리 메모.
    /// </summary>
    public string? Memo { get; set; }
}