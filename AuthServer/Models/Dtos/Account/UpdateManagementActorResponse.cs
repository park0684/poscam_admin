namespace poscam.AuthServer.Models.Dtos.Account;

/// <summary>
/// UpdateServer 관리자 요청을 수행하는 현재 사용자 정보.
/// </summary>
public class UpdateManagementActorResponse
{
    public int UserCode { get; set; }

    public string UserName { get; set; } = "";

    public int UserRole { get; set; }
}
