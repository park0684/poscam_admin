using System.Data;
using Dapper;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Dtos.Store;
using poscam.AuthServer.Models.Enums;

namespace poscam.AuthServer.Repositories;

/// <summary>
/// licensekeys 테이블 접근 Repository.
/// 
/// PC 캠 인증키 조회, 발급, 상태 변경을 담당한다.
/// 인증키가 사용 가능한지에 대한 업무 판단은 Service에서 처리한다.
/// </summary>
public class LicenseKeyRepository : RepositoryBase
{
    public LicenseKeyRepository(IDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// 라이선스 코드로 라이선스를 조회한다.
    /// </summary>
    public async Task<LicenseKey?> GetByCodeAsync(int licenseCode)
    {
        const string sql = @"
        SELECT
            lic_code     AS LicCode,
            lic_contract AS LicContract,
            lic_key      AS LicKey,
            lic_status   AS LicStatus,
            lic_rdate    AS LicRDate
        FROM licensekeys
        WHERE lic_code = @LicenseCode;
        ";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<LicenseKey>(
                sql,
                new { LicenseCode = licenseCode }));
    }

    /// <summary>
    /// 라이선스 키 문자열로 라이선스를 조회한다.
    /// 
    /// PC 캠 최초 인증 시 사용한다.
    /// </summary>
    public async Task<LicenseKey?> GetByKeyAsync(string licenseKey)
    {
        const string sql = @"
        SELECT
            lic_code     AS LicCode,
            lic_contract AS LicContract,
            lic_key      AS LicKey,
            lic_status   AS LicStatus,
            lic_rdate    AS LicRDate
        FROM licensekeys
        WHERE lic_key = @LicenseKey;
        ";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<LicenseKey>(
                sql,
                new { LicenseKey = licenseKey }));
    }

    /// <summary>
    /// 트랜잭션 내부에서 라이선스 키 문자열로 조회한다.
    /// 
    /// PC 캠 최초 인증은 장비 등록과 라이선스 상태 변경이 함께 처리되어야 하므로
    /// 트랜잭션 기반 조회가 필요하다.
    /// </summary>
    public async Task<LicenseKey?> GetByKeyAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        string licenseKey)
    {
        const string sql = @"
        SELECT
            lic_code     AS LicCode,
            lic_contract AS LicContract,
            lic_key      AS LicKey,
            lic_status   AS LicStatus,
            lic_rdate    AS LicRDate
        FROM licensekeys
        WHERE lic_key = @LicenseKey;
        ";

        return await connection.QueryFirstOrDefaultAsync<LicenseKey>(
            sql,
            new { LicenseKey = licenseKey },
            transaction);
    }

    /// <summary>
    /// 특정 계약에 발급된 라이선스 수량을 조회한다.
    /// 
    /// 계약의 PC 캠 허용 수량 초과 여부 판단에 사용한다.
    /// </summary>
    public async Task<int> CountByContractAsync(int contractCode)
    {
        const string sql = @"
        SELECT COUNT(1)
        FROM licensekeys
        WHERE lic_contract = @ContractCode;
        ";

        return await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(sql, new { ContractCode = contractCode }));
    }

    /// <summary>
    /// 특정 계약에 등록된 사용중 라이선스 수량을 조회한다.
    /// 
    /// PC 캠 장비 등록 수량 검증에 사용할 수 있다.
    /// </summary>
    public async Task<int> CountActivatedByContractAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        int contractCode)
    {
        const string sql = @"
        SELECT COUNT(1)
        FROM licensekeys
        WHERE lic_contract = @ContractCode
          AND lic_status = 1;
        ";

        return await connection.ExecuteScalarAsync<int>(
            sql,
            new { ContractCode = contractCode },
            transaction);
    }

    /// <summary>
    /// 라이선스 키가 이미 존재하는지 확인한다.
    /// 
    /// 라이선스 키 생성 시 중복 방지에 사용한다.
    /// </summary>
    public async Task<bool> ExistsKeyAsync(string licenseKey)
    {
        const string sql = @"
        SELECT COUNT(1)
        FROM licensekeys
        WHERE lic_key = @LicenseKey;
        ";

        var count = await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(sql, new { LicenseKey = licenseKey }));

        return count > 0;
    }

    /// <summary>
    /// 라이선스 키를 1개 등록한다.
    /// </summary>
    public async Task<int> InsertAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        LicenseKey licenseKey)
    {
        const string sql = @"
        INSERT INTO licensekeys
        (
            lic_contract,
            lic_key,
            lic_status,
            lic_rdate
        )
        VALUES
        (
            @LicContract,
            @LicKey,
            @LicStatus,
            NOW()
        );

        SELECT LAST_INSERT_ID();
        ";

        return await connection.ExecuteScalarAsync<int>(
            sql,
            licenseKey,
            transaction);
    }

    /// <summary>
    /// 라이선스 상태를 변경한다.
    /// 
    /// 예:
    /// - 최초 인증 성공 시 Ready → Activated
    /// - 관리자 장비 초기화 시 Activated → Reset
    /// - 폐기 시 Revoked
    /// </summary>
    public async Task<int> UpdateStatusAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        int licenseCode,
        int status)
    {
        const string sql = @"
        UPDATE licensekeys
        SET lic_status = @Status
        WHERE lic_code = @LicenseCode;
        ";

        return await connection.ExecuteAsync(
            sql,
            new
            {
                LicenseCode = licenseCode,
                Status = status
            },
            transaction);
    }

    /// <summary>
    /// 특정 매장의 전체 라이선스 목록을 조회한다.
    /// 
    /// 매장 상세 화면의 라이선스/인증정보 영역에서 사용한다.
    /// contracts를 통해 매장 기준 라이선스를 찾고,
    /// devices와 연결하여 등록된 장비 정보를 함께 표시한다.
    /// </summary>
    public async Task<List<StoreLicenseDto>> GetByStoreAsync(int storeCode)
    {
        const string sql = @"
SELECT
    lk.lic_code     AS LicenseCode,
    lk.lic_contract AS ContractCode,
    c.con_no        AS ContractNo,
    lk.lic_key      AS LicenseKey,
    lk.lic_status   AS LicenseStatus,
    d.dev_code      AS RegisteredDeviceCode,
    CASE
        WHEN d.dev_hwid IS NULL THEN NULL
        WHEN LENGTH(d.dev_hwid) <= 8 THEN d.dev_hwid
        ELSE CONCAT(LEFT(d.dev_hwid, 4), '****', RIGHT(d.dev_hwid, 4))
    END AS RegisteredHwidMasked,
    d.dev_pos       AS PosNo,
    lk.lic_rdate    AS RegisteredAt
FROM licensekeys lk
INNER JOIN contracts c
    ON lk.lic_contract = c.con_code
LEFT JOIN devices d
    ON lk.lic_code = d.dev_license
   AND d.dev_apptype = @PccamAppType
WHERE c.con_store = @StoreCode
ORDER BY lk.lic_rdate DESC, lk.lic_code DESC;
";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<StoreLicenseDto>(
                sql,
                new
                {
                    StoreCode = storeCode,
                    PccamAppType = (int)DeviceAppType.Pccam
                }));

        return result.ToList();
    }

    /// <summary>
    /// 특정 계약에 발급된 라이선스 목록을 조회한다.
    /// 
    /// 라이선스 발급 후 결과 확인 또는 계약 상세 화면에서 사용한다.
    /// </summary>
    public async Task<List<StoreLicenseDto>> GetByContractAsync(int contractCode)
    {
        const string sql = @"
SELECT
    lk.lic_code     AS LicenseCode,
    lk.lic_contract AS ContractCode,
    c.con_no        AS ContractNo,
    lk.lic_key      AS LicenseKey,
    lk.lic_status   AS LicenseStatus,
    d.dev_code      AS RegisteredDeviceCode,
    CASE
        WHEN d.dev_hwid IS NULL THEN NULL
        WHEN LENGTH(d.dev_hwid) <= 8 THEN d.dev_hwid
        ELSE CONCAT(LEFT(d.dev_hwid, 4), '****', RIGHT(d.dev_hwid, 4))
    END AS RegisteredHwidMasked,
    d.dev_pos       AS PosNo,
    lk.lic_rdate    AS RegisteredAt
FROM licensekeys lk
INNER JOIN contracts c
    ON lk.lic_contract = c.con_code
LEFT JOIN devices d
    ON lk.lic_code = d.dev_license
   AND d.dev_apptype = @PccamAppType
WHERE lk.lic_contract = @ContractCode
ORDER BY lk.lic_rdate DESC, lk.lic_code DESC;
";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<StoreLicenseDto>(
                sql,
                new
                {
                    ContractCode = contractCode,
                    PccamAppType = (int)DeviceAppType.Pccam
                }));

        return result.ToList();
    }

    /// <summary>
    /// 트랜잭션 내부에서 라이선스 코드로 라이선스를 조회한다.
    /// 
    /// 인증키 폐기 처리처럼
    /// 상태 변경과 로그 저장을 하나의 트랜잭션으로 묶을 때 사용한다.
    /// </summary>
    public async Task<LicenseKey?> GetByCodeAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        int licenseCode)
    {
        const string sql = @"
        SELECT
            lic_code     AS LicCode,
            lic_contract AS LicContract,
            lic_key      AS LicKey,
            lic_status   AS LicStatus,
            lic_rdate    AS LicRDate
        FROM licensekeys
        WHERE lic_code = @LicenseCode;
    ";

        return await connection.QueryFirstOrDefaultAsync<LicenseKey>(
            sql,
            new { LicenseCode = licenseCode },
            transaction);
    }
}