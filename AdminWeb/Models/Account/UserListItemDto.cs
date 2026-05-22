namespace poscam.AdminWeb.Models.Account;

/// <summary>
/// 담당자 선택 목록 DTO.
/// </summary>
public class UserListItemDto
{
    public int UserCode { get; set; }

    public int? PartnerCode { get; set; }

    public string? PartnerName { get; set; }

    public string UserId { get; set; } = "";

    public string UserName { get; set; } = "";

    public string? UserCell { get; set; }

    public string? UserEmail { get; set; }

    public int UserRole { get; set; }

    public int UserStatus { get; set; }
}