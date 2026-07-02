using poscam.AuthServer.Models.Enums;

namespace poscam.AuthServer.Repositories;

public interface IPartnerUserPermissionReader
{
    Task<bool> ExistsPermissionAsync(
        int userCode,
        PartnerUserPermissionType permission);

    Task<List<int>> GetPermissionCodesAsync(int userCode);
}
