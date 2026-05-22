using Dapper;
using poscam.AuthServer.Models.Dtos.Store;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;

namespace poscam.AuthServer.Repositories;

/// <summary>
/// store_user_assignments 테이블 접근 Repository.
/// 
/// 매장과 담당자 연결 정보를 관리한다.
/// 담당자가 볼 수 있는 매장 범위를 결정하는 핵심 Repository다.
/// </summary>
public class StoreAssignmentRepository : RepositoryBase
{
    public StoreAssignmentRepository(IDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// 특정 매장의 담당자 연결 목록을 조회한다.
    /// 매장 상세 화면에서 사용한다.
    /// </summary>
    public async Task<List<StoreAssignmentDto>> GetByStoreAsync(int storeCode)
    {
        const string sql = @"
        SELECT
            sua.sua_code        AS AssignmentCode,
            sua.store_code      AS StoreCode,
            sua.user_code       AS UserCode,
            u.user_name         AS UserName,
            u.user_cell         AS UserCell,
            u.user_email        AS UserEmail,
            sua.partner_code    AS PartnerCode,
            p.partner_name      AS PartnerName,
            sua.assignment_role AS AssignmentRole,
            sua.is_primary      AS IsPrimary,
            sua.status          AS Status,
            sua.assigned_at     AS AssignedAt
        FROM store_user_assignments sua
        INNER JOIN users u
            ON sua.user_code = u.user_code
        LEFT JOIN partners p
            ON sua.partner_code = p.partner_code
        WHERE sua.store_code = @StoreCode
          AND sua.status = @ActiveStatus
        ORDER BY sua.is_primary DESC, sua.assigned_at DESC, sua.sua_code DESC;
        ";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<StoreAssignmentDto>(
                sql,
                new
                {
                    StoreCode = storeCode,
                    ActiveStatus = (int)AssignmentStatus.Active
                }));

        return result.ToList();
    }

    /// <summary>
    /// 담당자 기준으로 배정된 매장 코드 목록을 조회한다.
    /// 담당자 권한의 매장 목록 필터링에 사용한다.
    /// </summary>
    public async Task<List<int>> GetAssignedStoreCodesByUserAsync(int userCode)
    {
        const string sql = @"
        SELECT store_code
        FROM store_user_assignments
        WHERE user_code = @UserCode
          AND status = @ActiveStatus;
        ";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<int>(
                sql,
                new
                {
                    UserCode = userCode,
                    ActiveStatus = (int)AssignmentStatus.Active
                }));

        return result.ToList();
    }

    /// <summary>
    /// 특정 사용자가 특정 매장에 접근할 수 있는지 확인한다.
    /// 담당자 권한 체크에 사용한다.
    /// 관리자는 이 메서드를 거치지 않고 전체 접근 허용 처리한다.
    /// </summary>
    public async Task<bool> CanAccessStoreAsync(int userCode, int storeCode)
    {
        const string sql = @"
        SELECT COUNT(1)
        FROM store_user_assignments
        WHERE user_code = @UserCode
          AND store_code = @StoreCode
          AND status = @ActiveStatus;
        ";

        var count = await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    UserCode = userCode,
                    StoreCode = storeCode,
                    ActiveStatus = (int)AssignmentStatus.Active
                }));

        return count > 0;
    }

    /// <summary>
    /// 동일 매장/사용자/역할의 활성 배정이 이미 존재하는지 확인한다.
    /// 중복 배정을 방지하기 위해 사용한다.
    /// </summary>
    public async Task<bool> ExistsActiveAssignmentAsync(
        int storeCode,
        int userCode,
        string assignmentRole)
    {
        const string sql = @"
        SELECT COUNT(1)
        FROM store_user_assignments
        WHERE store_code = @StoreCode
          AND user_code = @UserCode
          AND assignment_role = @AssignmentRole
          AND status = @ActiveStatus;
        ";

        var count = await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    StoreCode = storeCode,
                    UserCode = userCode,
                    AssignmentRole = assignmentRole,
                    ActiveStatus = (int)AssignmentStatus.Active
                }));

        return count > 0;
    }

    /// <summary>
    /// 매장 담당자 연결 정보를 등록한다.
    /// </summary>
    public async Task<int> InsertAsync(StoreUserAssignment assignment)
    {
        const string sql = @"
        INSERT INTO store_user_assignments
        (
            store_code,
            user_code,
            partner_code,
            assignment_role,
            is_primary,
            status,
            assigned_by,
            assigned_at
        )
        VALUES
        (
            @StoreCode,
            @UserCode,
            @PartnerCode,
            @AssignmentRole,
            @IsPrimary,
            @Status,
            @AssignedBy,
            NOW()
        );

        SELECT LAST_INSERT_ID();
        ";

        return await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(sql, assignment));
    }

    /// <summary>
    /// 매장 담당자 연결을 해제한다.
    /// 물리 삭제하지 않고 status를 Released로 변경한다.
    /// </summary>
    public async Task<int> ReleaseAsync(int assignmentCode)
    {
        const string sql = @"
        UPDATE store_user_assignments
        SET
            status = @ReleasedStatus,
            released_at = NOW()
        WHERE sua_code = @AssignmentCode;
        ";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(
                sql,
                new
                {
                    AssignmentCode = assignmentCode,
                    ReleasedStatus = (int)AssignmentStatus.Released
                }));
    }

    /// <summary>
    /// 특정 매장의 기존 대표 담당 플래그를 해제한다.
    /// 새 대표 담당자를 지정하기 전에 사용한다.
    /// </summary>
    public async Task<int> ClearPrimaryByStoreAsync(int storeCode)
    {
        const string sql = @"
        UPDATE store_user_assignments
        SET is_primary = 0
        WHERE store_code = @StoreCode
          AND status = @ActiveStatus;
        ";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(
                sql,
                new
                {
                    StoreCode = storeCode,
                    ActiveStatus = (int)AssignmentStatus.Active
                }));
    }

    /// <summary>
    /// 특정 사용자가 특정 매장에 특정 역할로 배정되어 있는지 확인한다.
    /// 
    /// 예:
    /// - MANAGE 역할 담당자만 PC캠 초기화 허용
    /// </summary>
    public async Task<bool> HasActiveAssignmentRoleAsync(
        int userCode,
        int storeCode,
        string assignmentRole)
    {
        const string sql = @"
        SELECT COUNT(1)
        FROM store_user_assignments
        WHERE user_code = @UserCode
          AND store_code = @StoreCode
          AND assignment_role = @AssignmentRole
          AND status = @ActiveStatus;
";

        var count = await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    UserCode = userCode,
                    StoreCode = storeCode,
                    AssignmentRole = assignmentRole,
                    ActiveStatus = (int)AssignmentStatus.Active
                }));

        return count > 0;
    }

    /// <summary>
    /// 담당자 연결 코드로 연결 정보를 조회한다.
    /// 권한 판단 시 store_code, partner_code 확인에 사용한다.
    /// </summary>
    public async Task<StoreUserAssignment?> GetByCodeAsync(int assignmentCode)
    {
        const string sql = @"
        SELECT
            sua_code AS AssignmentCode,
            store_code      AS StoreCode,
            user_code       AS UserCode,
            partner_code    AS PartnerCode,
            assignment_role AS AssignmentRole,
            is_primary      AS IsPrimary,
            status          AS Status,
            assigned_by     AS AssignedBy,
            assigned_at     AS AssignedAt
        FROM store_user_assignments
        WHERE sua_code = @AssignmentCode;
";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<StoreUserAssignment>(
                sql,
                new
                {
                    AssignmentCode = assignmentCode
                }));
    }

    /// <summary>
    /// 특정 매장의 대표 담당 파트너사 코드를 조회한다.
    /// 
    /// 계약은 파트너사 기준으로 관리되며,
    /// 매장과 연결된 계약을 생성할 경우
    /// 해당 매장의 대표 담당 파트너사 코드가 계약의 ConPartner가 된다.
    /// 
    /// 조회 기준:
    /// - 동일 매장(store_code)
    /// - 활성 배정(status = Active)
    /// - 대표 담당(is_primary = 1)
    /// - 파트너사 코드가 존재하는 배정(partner_code IS NOT NULL)
    /// </summary>
    /// <param name="storeCode">매장 코드</param>
    /// <returns>
    /// 대표 담당 파트너사 코드.
    /// 존재하지 않으면 null.
    /// </returns>
    public async Task<int?> GetPrimaryPartnerCodeByStoreAsync(int storeCode)
    {
        const string sql = @"
        SELECT partner_code
        FROM store_user_assignments
        WHERE store_code = @StoreCode
          AND is_primary = 1
          AND status = @ActiveStatus
          AND partner_code IS NOT NULL
        ORDER BY assigned_at DESC, sua_code DESC
        LIMIT 1;
    ";

        return await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int?>(sql, new
            {
                StoreCode = storeCode,
                ActiveStatus = (int)AssignmentStatus.Active
            }));
    }
}