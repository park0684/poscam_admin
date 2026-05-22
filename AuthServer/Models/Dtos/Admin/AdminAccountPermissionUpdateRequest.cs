namespace poscam.AuthServer.Models.Dtos.Admin;

/// <summary>
/// 관리자 세부 권한 수정 요청 DTO.
/// 
/// 전달된 권한 코드 목록으로 해당 관리자의 권한을 교체한다.
/// </summary>
public class AdminAccountPermissionUpdateRequest
{
    /// <summary>
    /// 권한을 수정할 관리자 user_code.
    /// </summary>
    public int UserCode { get; set; }

    /// <summary>
    /// 부여할 관리자 권한 코드 목록.
    /// DB에는 숫자 값만 저장한다.
    /// </summary>
    public List<int> PermissionCodes { get; set; } = new();
}