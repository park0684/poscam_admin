namespace poscam.AuthServer.Models.Dtos.UserManage;

/// <summary>
/// 직원/담당자 정보 수정 요청 DTO.
/// 
/// System/Admin:
/// - 파트너사, 담당자명, 연락처, 이메일 수정 가능
/// 
/// PartnerUser:
/// - 본인 계정의 연락처, 이메일만 직접 수정 가능
/// - 아이디, 담당자명, 파트너사는 직접 수정 불가
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