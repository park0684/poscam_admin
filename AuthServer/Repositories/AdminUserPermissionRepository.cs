using Dapper;
using poscam.AuthServer.Models.Enums;

namespace poscam.AuthServer.Repositories;

/// <summary>
/// 관리자 세부 권한 Repository.
/// 
/// admin_user_permissions 테이블을 사용하여
/// 관리자 계정별 권한 보유 여부와 권한 코드 목록을 조회한다.
/// 
/// DB 저장 기준:
/// - apu_user: users.user_code
/// - apu_permission: AdminPermissionType의 int 값
/// </summary>
public class AdminUserPermissionRepository : RepositoryBase
{
    public AdminUserPermissionRepository(IDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <summary>
    /// 특정 사용자가 특정 관리자 권한을 보유하고 있는지 확인한다.
    /// 
    /// System 계정 여부는 Service에서 먼저 판단하며,
    /// 이 Repository는 Admin 계정의 실제 권한 등록 여부만 확인한다.
    /// </summary>
    public async Task<bool> ExistsPermissionAsync(
        int userCode,
        AdminPermissionType permission)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM admin_user_permissions
            WHERE apu_user = @UserCode
              AND apu_permission = @PermissionCode;
        ";

        var count = await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    UserCode = userCode,
                    PermissionCode = (int)permission
                }));

        return count > 0;
    }

    /// <summary>
    /// 특정 관리자 계정에 부여된 권한 코드 목록을 조회한다.
    /// 
    /// 권한명은 DB에서 관리하지 않으며,
    /// 반환값은 AdminPermissionType의 int 값이다.
    /// </summary>
    public async Task<List<int>> GetPermissionCodesAsync(int userCode)
    {
        const string sql = @"
            SELECT apu_permission
            FROM admin_user_permissions
            WHERE apu_user = @UserCode
            ORDER BY apu_permission;
        ";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<int>(
                sql,
                new
                {
                    UserCode = userCode
                }));

        return result.ToList();
    }

    /// <summary>
    /// 특정 관리자 계정의 권한을 전달받은 권한 목록으로 교체한다.
    /// 
    /// 기존 권한은 모두 삭제하고,
    /// permissionCodes에 포함된 권한 코드만 새로 등록한다.
    /// </summary>
    public async Task ReplacePermissionsAsync(
        int userCode,
        List<int> permissionCodes,
        int changedBy)
    {
        await WithConnectionAsync(async conn =>
        {
            using var transaction = conn.BeginTransaction();

            try
            {
                const string deleteSql = @"
                DELETE FROM admin_user_permissions
                WHERE apu_user = @UserCode;
            ";

                await conn.ExecuteAsync(
                    deleteSql,
                    new
                    {
                        UserCode = userCode
                    },
                    transaction);

                if (permissionCodes != null && permissionCodes.Count > 0)
                {
                    const string insertSql = @"
                    INSERT INTO admin_user_permissions
                    (
                        apu_user,
                        apu_permission,
                        apu_created_by
                    )
                    VALUES
                    (
                        @UserCode,
                        @PermissionCode,
                        @CreatedBy
                    );
                ";

                    foreach (var permissionCode in permissionCodes.Distinct())
                    {
                        await conn.ExecuteAsync(
                            insertSql,
                            new
                            {
                                UserCode = userCode,
                                PermissionCode = permissionCode,
                                CreatedBy = changedBy
                            },
                            transaction);
                    }
                }

                transaction.Commit();

                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        });
    }
}