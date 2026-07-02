namespace poscam.AdminWeb.Models.UserManage;

public sealed class PartnerUserPermissionUpdateRequest
{
    public int UserCode { get; set; }
    public List<int> PermissionCodes { get; set; } = new();
}
