namespace poscam.AuthServer.Models.Dtos.Account;

/// <summary>
/// 담당자 승인 응답 DTO.
/// </summary>
public class UserApproveResponse
{
    public int UserCode { get; set; }

    public bool Approved { get; set; }

    public DateTime ApprovedAt { get; set; }
}