namespace poscam.AdminWeb.Models.Admin;

/// <summary>
/// 관리자 세부 권한 수정 요청 DTO.
/// 
/// AuthServer의 api/admin/accounts/{userCode}/permissions PUT 요청에 사용한다.
/// </summary>
public class AdminAccountPermissionUpdateRequest
{
    /// <summary>
    /// 권한을 수정할 관리자 user_code.
    /// </summary>
    public int UserCode { get; set; }

    /// <summary>
    /// 부여할 관리자 권한 코드 목록.
    /// </summary>
    public List<int> PermissionCodes { get; set; } = new();
}