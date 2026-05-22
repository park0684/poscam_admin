namespace poscam.AuthServer.Models.Dtos.Account;

/// <summary>
/// 담당자 승인 요청 DTO.
/// 
/// 실제 운영에서는 승인자 정보는 로그인 토큰에서 가져오는 것이 맞다.
/// 1차 구현에서는 테스트 편의를 위해 ApprovedBy를 받을 수 있다.
/// </summary>
public class UserApproveRequest
{
    /// <summary>
    /// 승인할 사용자 코드.
    /// </summary>
    public int UserCode { get; set; }

    /// <summary>
    /// 승인하는 관리자 user_code.
    /// 추후 토큰 기반으로 대체 가능.
    /// </summary>
    public int ApprovedBy { get; set; }
}