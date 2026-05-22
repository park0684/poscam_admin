namespace poscam.AuthServer.Models.Dtos.UserManage;

/// <summary>
/// 직원/담당자 정보 수정 요청 DTO.
/// 
/// 실제 정보 수정은 관리자만 수행한다.
/// 담당자는 직접 수정하지 않고, 별도 변경 요청을 등록하는 구조로 처리한다.
/// </summary>
public class UserUpdateRequest
{
    /// <summary>
    /// 수정 대상 사용자 코드.
    /// Route에도 userCode가 포함되지만, Body에도 넣어 검증에 활용한다.
    /// </summary>
    public int UserCode { get; set; }

    /// <summary>
    /// 소속 파트너사 코드.
    /// 관리자만 변경 가능.
    /// </summary>
    public int? PartnerCode { get; set; }

    public string UserName { get; set; } = "";

    public string? UserCell { get; set; }

    public string? UserEmail { get; set; }
}