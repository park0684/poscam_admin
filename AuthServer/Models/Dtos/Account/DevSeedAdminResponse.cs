namespace poscam.AuthServer.Models.Dtos.Account;

/// <summary>
/// 개발용 초기 관리자 계정 생성 응답 DTO.
/// </summary>
public class DevSeedAdminResponse
{
    public int UserCode { get; set; }

    public string UserId { get; set; } = "";

    public string Password { get; set; } = "";

    public bool Created { get; set; }

    public bool Updated { get; set; }
}