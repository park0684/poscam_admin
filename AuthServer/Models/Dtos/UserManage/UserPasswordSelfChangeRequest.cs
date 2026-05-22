namespace poscam.AuthServer.Models.Dtos.UserManage;

/// <summary>
/// 담당자 본인 비밀번호 변경 요청 DTO.
/// 
/// 관리자 초기화와 다르게 현재 비밀번호 확인이 필요합니다.
/// </summary>
public class UserPasswordSelfChangeRequest
{
    /// <summary>
    /// 현재 사용 중인 비밀번호.
    /// </summary>
    public string? CurrentPassword { get; set; }

    /// <summary>
    /// 새로 변경할 비밀번호.
    /// </summary>
    public string? NewPassword { get; set; }
}