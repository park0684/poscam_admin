using System.Data;
using Dapper;
using poscam.AuthServer.Models.Enums;

namespace poscam.AuthServer.Repositories;

public class PartnerUserPermissionRepository : RepositoryBase, IPartnerUserPermissionReader
{
    public PartnerUserPermissionRepository(IDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<bool> ExistsPermissionAsync(
        int userCode,
        PartnerUserPermissionType permission)
    {
        const string sql = @"
SELECT COUNT(1)
FROM partner_user_permissions
WHERE pup_user = @UserCode
  AND pup_permission = @PermissionCode;
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

    public async Task<List<int>> GetPermissionCodesAsync(int userCode)
    {
        const string sql = @"
SELECT pup_permission
FROM partner_user_permissions
WHERE pup_user = @UserCode
ORDER BY pup_permission;
";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<int>(sql, new { UserCode = userCode }));

        return result.ToList();
    }

    public async Task<int> ReplacePermissionsAsync(
        int userCode,
        List<int> permissionCodes,
        int changedBy)
    {
        return await WithConnectionAsync(async conn =>
        {
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            using var transaction = conn.BeginTransaction();

            try
            {
                const string selectBeforeSql = @"
SELECT pup_permission
FROM partner_user_permissions
WHERE pup_user = @UserCode
ORDER BY pup_permission;
";

                var beforePermissions = (await conn.QueryAsync<int>(
                        selectBeforeSql,
                        new { UserCode = userCode },
                        transaction))
                    .ToList();

                var afterPermissions = permissionCodes
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                if (beforePermissions.SequenceEqual(afterPermissions))
                {
                    transaction.Commit();
                    return 0;
                }

                const string deleteSql = @"
DELETE FROM partner_user_permissions
WHERE pup_user = @UserCode;
";

                var affected = await conn.ExecuteAsync(
                    deleteSql,
                    new { UserCode = userCode },
                    transaction);

                const string insertSql = @"
INSERT INTO partner_user_permissions
(
    pup_user,
    pup_permission,
    pup_created_at,
    pup_created_by
)
VALUES
(
    @UserCode,
    @PermissionCode,
    NOW(),
    @ChangedBy
);
";

                foreach (var permissionCode in afterPermissions)
                {
                    affected += await conn.ExecuteAsync(
                        insertSql,
                        new
                        {
                            UserCode = userCode,
                            PermissionCode = permissionCode,
                            ChangedBy = changedBy
                        },
                        transaction);
                }

                const string logSql = @"
INSERT INTO partner_user_permission_logs
(
    pupl_user,
    pupl_changed_by,
    pupl_before_permissions,
    pupl_after_permissions,
    pupl_changed_at
)
VALUES
(
    @UserCode,
    @ChangedBy,
    @BeforePermissions,
    @AfterPermissions,
    NOW()
);
";

                await conn.ExecuteAsync(
                    logSql,
                    new
                    {
                        UserCode = userCode,
                        ChangedBy = changedBy,
                        BeforePermissions = string.Join(",", beforePermissions),
                        AfterPermissions = string.Join(",", afterPermissions)
                    },
                    transaction);

                transaction.Commit();
                return affected;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        });
    }

    public async Task<int> InsertDefaultPermissionsAsync(
        int userCode,
        int changedBy)
    {
        var defaults = Enum.GetValues<PartnerUserPermissionType>()
            .Select(x => (int)x)
            .ToList();

        return await ReplacePermissionsAsync(
            userCode,
            defaults,
            changedBy);
    }
}
