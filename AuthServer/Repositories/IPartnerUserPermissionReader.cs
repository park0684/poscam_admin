namespace poscam.AuthServer.Repositories;

public interface IPartnerUserPermissionReader
{
    Task<List<int>> GetPermissionCodesAsync(int userCode);
}
