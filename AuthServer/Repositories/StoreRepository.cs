using System.Data;
using Dapper;
using poscam.AuthServer.Models.Dtos.Store;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;

namespace poscam.AuthServer.Repositories;

/// <summary>
/// stores 테이블 접근 Repository.
/// 
/// 매장 조회, 매장 등록, 매장 수정, 매장 목록 조회를 담당한다.
/// 권한 판단은 Service에서 처리한다.
/// </summary>
public class StoreRepository : RepositoryBase
{
    public StoreRepository(IDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// 매장 코드로 매장을 조회한다.
    /// </summary>
    public async Task<Store?> GetByCodeAsync(int storeCode)
    {
        const string sql = @"
SELECT
    store_code       AS StoreCode,
    store_id         AS StoreId,
    store_password   AS StorePassword,
    store_name       AS StoreName,
    store_biznum     AS StoreBizNum,
    store_owner_name AS StoreOwnerName,
    store_tel        AS StoreTel,
    store_email      AS StoreEmail,
    store_zipcode    AS StoreZipcode,
    store_address1   AS StoreAddress1,
    store_address2   AS StoreAddress2,
    store_memo       AS StoreMemo,
    store_status     AS StoreStatus,
    store_rdate      AS StoreRDate,
    store_udate      AS StoreUDate
FROM stores
WHERE store_code = @StoreCode;
";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<Store>(
                sql,
                new { StoreCode = storeCode }));
    }

    /// <summary>
    /// 트랜잭션 내부에서 매장 코드로 매장을 조회한다.
    /// </summary>
    public async Task<Store?> GetByCodeAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        int storeCode)
    {
        const string sql = @"
SELECT
    store_code       AS StoreCode,
    store_id         AS StoreId,
    store_password   AS StorePassword,
    store_name       AS StoreName,
    store_biznum     AS StoreBizNum,
    store_owner_name AS StoreOwnerName,
    store_tel        AS StoreTel,
    store_email      AS StoreEmail,
    store_zipcode    AS StoreZipcode,
    store_address1   AS StoreAddress1,
    store_address2   AS StoreAddress2,
    store_memo       AS StoreMemo,
    store_status     AS StoreStatus,
    store_rdate      AS StoreRDate,
    store_udate      AS StoreUDate
FROM stores
WHERE store_code = @StoreCode;
";

        return await connection.QueryFirstOrDefaultAsync<Store>(
            sql,
            new { StoreCode = storeCode },
            transaction);
    }

    /// <summary>
    /// 매장 로그인 ID로 매장을 조회한다.
    /// 캠뷰어 로그인 검증 시 사용한다.
    /// </summary>
    public async Task<Store?> GetByLoginIdAsync(int storeCode, string storeId)
    {
        const string sql = @"
SELECT
    store_code       AS StoreCode,
    store_id         AS StoreId,
    store_password   AS StorePassword,
    store_name       AS StoreName,
    store_biznum     AS StoreBizNum,
    store_owner_name AS StoreOwnerName,
    store_tel        AS StoreTel,
    store_email      AS StoreEmail,
    store_zipcode    AS StoreZipcode,
    store_address1   AS StoreAddress1,
    store_address2   AS StoreAddress2,
    store_memo       AS StoreMemo,
    store_status     AS StoreStatus,
    store_rdate      AS StoreRDate,
    store_udate      AS StoreUDate
FROM stores
WHERE store_code = @StoreCode
  AND store_id = @StoreId;
";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<Store>(
                sql,
                new
                {
                    StoreCode = storeCode,
                    StoreId = storeId
                }));
    }

    /// <summary>
    /// 매장 ID가 이미 존재하는지 확인한다.
    /// 매장 ID는 백엔드에서 생성하지만, 최종 중복 확인은 DB에서 한다.
    /// </summary>
    public async Task<bool> ExistsStoreIdAsync(string storeId)
    {
        const string sql = @"
SELECT COUNT(1)
FROM stores
WHERE store_id = @StoreId;
";

        var count = await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(
                sql,
                new { StoreId = storeId }));

        return count > 0;
    }

    /// <summary>
    /// 현재 DB에 저장된 매장 ID 중 가장 큰 값을 조회한다.
    /// 매장 ID 증가값 계산에 사용한다.
    /// </summary>
    public async Task<string?> GetMaxStoreIdAsync()
    {
        const string sql = @"
        SELECT store_id
        FROM stores
        WHERE store_id REGEXP '^[A-Z]{2}[0-9]{4}$'
          AND RIGHT(store_id, 4) <> '0000'
        ORDER BY store_id DESC
        LIMIT 1;
        ";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<string>(sql));
    }

    /// <summary>
    /// 신규 매장을 등록한다.
    /// store_code는 AUTO_INCREMENT이고, store_id는 백엔드에서 생성한 값을 저장한다.
    /// </summary>
    public async Task<int> InsertAsync(Store store)
    {
        const string sql = @"
INSERT INTO stores
(
    store_id,
    store_password,
    store_name,
    store_biznum,
    store_owner_name,
    store_tel,
    store_email,
    store_zipcode,
    store_address1,
    store_address2,
    store_memo,
    store_status,
    store_rdate
)
VALUES
(
    @StoreId,
    @StorePassword,
    @StoreName,
    @StoreBizNum,
    @StoreOwnerName,
    @StoreTel,
    @StoreEmail,
    @StoreZipcode,
    @StoreAddress1,
    @StoreAddress2,
    @StoreMemo,
    @StoreStatus,
    NOW()
);

SELECT LAST_INSERT_ID();
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(sql, store));
    }

    /// <summary>
    /// 트랜잭션 내부에서 신규 매장을 등록한다.
    /// </summary>
    public async Task<int> InsertAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        Store store)
    {
        const string sql = @"
INSERT INTO stores
(
    store_id,
    store_password,
    store_name,
    store_biznum,
    store_owner_name,
    store_tel,
    store_email,
    store_zipcode,
    store_address1,
    store_address2,
    store_memo,
    store_status,
    store_rdate
)
VALUES
(
    @StoreId,
    @StorePassword,
    @StoreName,
    @StoreBizNum,
    @StoreOwnerName,
    @StoreTel,
    @StoreEmail,
    @StoreZipcode,
    @StoreAddress1,
    @StoreAddress2,
    @StoreMemo,
    @StoreStatus,
    NOW()
);

SELECT LAST_INSERT_ID();
";

        return await connection.ExecuteScalarAsync<int>(
            sql,
            store,
            transaction);
    }

    /// <summary>
    /// 매장 기본정보를 수정한다.
    /// 매장 ID와 비밀번호는 여기서 수정하지 않는다.
    /// </summary>
    public async Task<int> UpdateAsync(Store store)
    {
        const string sql = @"
UPDATE stores
SET
    store_name = @StoreName,
    store_biznum = @StoreBizNum,
    store_owner_name = @StoreOwnerName,
    store_tel = @StoreTel,
    store_email = @StoreEmail,
    store_zipcode = @StoreZipcode,
    store_address1 = @StoreAddress1,
    store_address2 = @StoreAddress2,
    store_memo = @StoreMemo,
    store_status = @StoreStatus,
    store_udate = NOW()
WHERE store_code = @StoreCode;
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(sql, store));
    }

    /// <summary>
    /// 관리자용 매장 목록을 조회한다.
    /// 관리자는 전체 매장을 볼 수 있다.
    /// </summary>
    public async Task<List<StoreListItemDto>> GetListForAdminAsync()
    {
        const string sql = @"
SELECT
    s.store_code       AS StoreCode,
    s.store_id         AS StoreId,
    s.store_name       AS StoreName,
    s.store_biznum     AS StoreBizNum,
    s.store_owner_name AS StoreOwnerName,
    s.store_tel        AS StoreTel,
    s.store_address1   AS StoreAddress1,
    s.store_address2   AS StoreAddress2,
    s.store_status     AS StoreStatus,
    primary_partner.partner_name AS PrimaryPartnerName,
    primary_user.user_name       AS PrimaryUserName,
    IFNULL(contract_count.contract_count, 0) AS ContractCount,
    IFNULL(pccam_count.device_count, 0)      AS PccamDeviceCount,
    IFNULL(viewer_count.device_count, 0)     AS ViewerDeviceCount,
    s.store_rdate      AS RegisteredAt
FROM stores s
LEFT JOIN (
    SELECT
        sua.store_code,
        MIN(sua.partner_code) AS partner_code
    FROM store_user_assignments sua
    WHERE sua.status = @AssignmentActive
      AND sua.is_primary = 1
    GROUP BY sua.store_code
) primary_assignment
    ON s.store_code = primary_assignment.store_code
LEFT JOIN partners primary_partner
    ON primary_assignment.partner_code = primary_partner.partner_code
LEFT JOIN (
    SELECT
        sua.store_code,
        MIN(sua.user_code) AS user_code
    FROM store_user_assignments sua
    WHERE sua.status = @AssignmentActive
      AND sua.is_primary = 1
    GROUP BY sua.store_code
) primary_assignment_user
    ON s.store_code = primary_assignment_user.store_code
LEFT JOIN users primary_user
    ON primary_assignment_user.user_code = primary_user.user_code
LEFT JOIN (
    SELECT
        con_store,
        COUNT(1) AS contract_count
    FROM contracts
    GROUP BY con_store
) contract_count
    ON s.store_code = contract_count.con_store
LEFT JOIN (
    SELECT
        dev_store,
        COUNT(1) AS device_count
    FROM devices
    WHERE dev_apptype = @PccamAppType
    GROUP BY dev_store
) pccam_count
    ON s.store_code = pccam_count.dev_store
LEFT JOIN (
    SELECT
        dev_store,
        COUNT(1) AS device_count
    FROM devices
    WHERE dev_apptype = @ViewerAppType
    GROUP BY dev_store
) viewer_count
    ON s.store_code = viewer_count.dev_store
ORDER BY s.store_rdate DESC, s.store_code DESC;
";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<StoreListItemDto>(
                sql,
                new
                {
                    AssignmentActive = (int)AssignmentStatus.Active,
                    PccamAppType = (int)DeviceAppType.Pccam,
                    ViewerAppType = (int)DeviceAppType.Viewer
                }));

        return result.ToList();
    }

    /// <summary>
    /// 파트너사 기준 매장 목록을 조회한다.
    /// 
    /// 담당자 개인 기준이 아니라, 로그인 사용자가 소속된 partner_code 기준으로 조회한다.
    /// 같은 파트너사에 속한 여러 담당자가 동일 매장에 연결되어 있어도
    /// 매장이 중복 표시되지 않도록 partner_access에서 store_code를 GROUP BY 처리한다.
    /// </summary>
    public async Task<List<StoreListItemDto>> GetListForPartnerAsync(int partnerCode)
    {
        const string sql = @"
SELECT
    s.store_code       AS StoreCode,
    s.store_id         AS StoreId,
    s.store_name       AS StoreName,
    s.store_biznum     AS StoreBizNum,
    s.store_owner_name AS StoreOwnerName,
    s.store_tel        AS StoreTel,
    s.store_address1   AS StoreAddress1,
    s.store_address2   AS StoreAddress2,
    s.store_status     AS StoreStatus,
    primary_partner.partner_name AS PrimaryPartnerName,
    primary_user.user_name       AS PrimaryUserName,
    IFNULL(contract_count.contract_count, 0) AS ContractCount,
    IFNULL(pccam_count.device_count, 0)      AS PccamDeviceCount,
    IFNULL(viewer_count.device_count, 0)     AS ViewerDeviceCount,
    s.store_rdate      AS RegisteredAt
FROM stores s
INNER JOIN store_user_assignments access_sua
    ON s.store_code = access_sua.store_code
   AND access_sua.partner_code = @PartnerCode
   AND access_sua.status = @AssignmentActive
LEFT JOIN (
    SELECT
        sua.store_code,
        MIN(sua.partner_code) AS partner_code
    FROM store_user_assignments sua
    WHERE sua.status = @AssignmentActive
      AND sua.is_primary = 1
    GROUP BY sua.store_code
) primary_assignment
    ON s.store_code = primary_assignment.store_code
LEFT JOIN partners primary_partner
    ON primary_assignment.partner_code = primary_partner.partner_code
LEFT JOIN (
    SELECT
        sua.store_code,
        MIN(sua.user_code) AS user_code
    FROM store_user_assignments sua
    WHERE sua.status = @AssignmentActive
      AND sua.is_primary = 1
    GROUP BY sua.store_code
) primary_assignment_user
    ON s.store_code = primary_assignment_user.store_code
LEFT JOIN users primary_user
    ON primary_assignment_user.user_code = primary_user.user_code
LEFT JOIN (
    SELECT
        con_store,
        COUNT(1) AS contract_count
    FROM contracts
    GROUP BY con_store
) contract_count
    ON s.store_code = contract_count.con_store
LEFT JOIN (
    SELECT
        dev_store,
        COUNT(1) AS device_count
    FROM devices
    WHERE dev_apptype = @PccamAppType
    GROUP BY dev_store
) pccam_count
    ON s.store_code = pccam_count.dev_store
LEFT JOIN (
    SELECT
        dev_store,
        COUNT(1) AS device_count
    FROM devices
    WHERE dev_apptype = @ViewerAppType
    GROUP BY dev_store
) viewer_count
    ON s.store_code = viewer_count.dev_store
GROUP BY s.store_code
ORDER BY s.store_rdate DESC, s.store_code DESC;
";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<StoreListItemDto>(
                sql,
                new
                {
                    PartnerCode = partnerCode,
                    AssignmentActive = (int)AssignmentStatus.Active,
                    PccamAppType = (int)DeviceAppType.Pccam,
                    ViewerAppType = (int)DeviceAppType.Viewer
                }));

        return result.ToList(); ;
    }

    /// <summary>
    /// 매장 상세 화면의 기본정보를 조회한다.
    /// 계약, 라이선스, 장비, 설정 정보는 각 Repository에서 별도로 조회한다.
    /// </summary>
    public async Task<StoreDetailDto?> GetDetailBaseAsync(int storeCode)
    {
        const string sql = @"
SELECT
    store_code       AS StoreCode,
    store_id         AS StoreId,
    store_name       AS StoreName,
    store_biznum     AS StoreBizNum,
    store_owner_name AS StoreOwnerName,
    store_tel        AS StoreTel,
    store_email      AS StoreEmail,
    store_zipcode    AS StoreZipcode,
    store_address1   AS StoreAddress1,
    store_address2   AS StoreAddress2,
    store_memo       AS StoreMemo,
    store_status     AS StoreStatus,
    store_rdate      AS RegisteredAt,
    store_udate      AS UpdatedAt
FROM stores
WHERE store_code = @StoreCode;
";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<StoreDetailDto>(
                sql,
                new { StoreCode = storeCode }));
    }

    public async Task<StoreUsageSummaryDto> GetUsageSummaryAsync(int storeCode)
    {
        const string sql = @"
SELECT @StoreCode AS StoreCode, SUM(con_pcc) as PccamUseCount, SUM(con_view) as ViewerUseCount FROM contracts WHERE con_store = 1 AND con_status  GROUP BY con_store";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstAsync<StoreUsageSummaryDto>(
                sql,
                new
                {
                    StoreCode = storeCode
                }));
    }

    /// <summary>
    /// 파트너사 기준으로 특정 매장에 접근 가능한지 확인한다.
    ///
    /// 기존 담당자 개인 기준:
    /// - user_code + store_code
    ///
    /// 변경된 매장 조회/상세 기준:
    /// - partner_code + store_code
    ///
    /// 즉, 로그인 담당자가 직접 배정된 매장이 아니더라도
    /// 같은 파트너사에 연결된 매장이면 접근 가능하다.
    /// </summary>
    /// <param name="partnerCode"></param>
    /// <param name="storeCode"></param>
    /// <returns></returns>
    public async Task<bool> CanPartnerAccessStoreAsync(
    int partnerCode,
    int storeCode)
    {
        const string sql = @"
SELECT COUNT(1)
FROM store_user_assignments
WHERE partner_code = @PartnerCode
  AND store_code = @StoreCode
  AND status = @ActiveStatus;
";

        var count = await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    PartnerCode = partnerCode,
                    StoreCode = storeCode,
                    ActiveStatus = (int)AssignmentStatus.Active
                }));

        return count > 0;
    }

    /// <summary>
    /// 파트너사 기준으로 연결된 활성 매장 코드 목록을 조회한다.
    /// 같은 파트너사의 여러 담당자가 동일 매장에 연결되어 있어도
    /// store_code는 중복 없이 반환한다.
    /// </summary>
    public async Task<List<int>> GetAssignedStoreCodesByPartnerAsync(int partnerCode)
    {
        const string sql = @"
SELECT DISTINCT store_code
FROM store_user_assignments
WHERE partner_code = @PartnerCode
  AND status = @ActiveStatus;
";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<int>(
                sql,
                new
                {
                    PartnerCode = partnerCode,
                    ActiveStatus = (int)AssignmentStatus.Active
                }));

        return result.ToList();
    }
}