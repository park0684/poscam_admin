namespace poscam.AuthServer.Models.Dtos.Account;

/// <summary>
/// 승인 대기 사용자 목록 DTO.
/// </summary>
public class UserPendingListItemDto
{
    public int UserCode { get; set; }

    public int? PartnerCode { get; set; }

    public string? PartnerName { get; set; }

    public string UserId { get; set; } = "";

    public string UserName { get; set; } = "";

    public string? UserCell { get; set; }

    public string? UserEmail { get; set; }

    public DateTime RegisteredAt { get; set; }
}