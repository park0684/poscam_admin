namespace poscam.AuthServer.Repositories;

/// <summary>
/// 관리자에게 현재 부여된 세부 권한 코드 조회 계약.
/// </summary>
public interface IAdminUserPermissionReader
{
    Task<List<int>> GetPermissionCodesAsync(int userCode);
}
