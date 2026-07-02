namespace poscam.AuthServer.Models.Dtos.UserManage;

public class PartnerUserPermissionUpdateRequest
{
    public int UserCode { get; set; }
    public List<int> PermissionCodes { get; set; } = new();
}
