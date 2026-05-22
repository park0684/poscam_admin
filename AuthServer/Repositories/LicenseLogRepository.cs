using System.Data;
using Dapper;
using poscam.AuthServer.Models.Entities;

namespace poscam.AuthServer.Repositories;

/// <summary>
/// licenselog 테이블 접근 Repository.
/// 
/// 라이선스 발급, 활성화, 초기화, 폐기 등의 작업 이력을 저장한다.
/// </summary>
public class LicenseLogRepository : RepositoryBase
{
    public LicenseLogRepository(IDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// 라이선스 로그를 저장한다.
    /// 
    /// lig_code는 VARCHAR(20)이므로 Service에서 생성해서 전달한다.
    /// 예: L202605030001
    /// </summary>
    public async Task<int> InsertAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        LicenseLog log)
    {
        const string sql = @"
        INSERT INTO licenselog
        (
            lig_code,
            lig_license,
            lig_store,
            lig_hwid,
            lig_action_type,
            lig_reason,
            lig_rdate
        )
        VALUES
        (
            @LigCode,
            @LigLicense,
            @LigStore,
            @LigHwid,
            @LigActionType,
            @LigReason,
            NOW()
        );
        ";

        return await connection.ExecuteAsync(sql, log, transaction);
    }

    /// <summary>
    /// 라이선스 로그를 단독으로 저장한다.
    /// 
    /// 트랜잭션이 필요 없는 단순 기록 시 사용할 수 있다.
    /// </summary>
    public async Task<int> InsertAsync(LicenseLog log)
    {
        const string sql = @"
        INSERT INTO licenselog
        (
            lig_code,
            lig_license,
            lig_store,
            lig_hwid,
            lig_action_type,
            lig_reason,
            lig_rdate
        )
        VALUES
        (
            @LigCode,
            @LigLicense,
            @LigStore,
            @LigHwid,
            @LigActionType,
            @LigReason,
            NOW()
        );
        ";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(sql, log));
    }

    
}