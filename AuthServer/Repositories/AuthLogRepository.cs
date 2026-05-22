using System.Data;
using Dapper;
using poscam.AuthServer.Models.Entities;

namespace poscam.AuthServer.Repositories;

/// <summary>
/// auth_logs 테이블 접근 Repository.
/// 
/// 인증 요청, 인증 실패, 하트비트, 설정 다운로드 등의 로그를 저장한다.
/// </summary>
public class AuthLogRepository : RepositoryBase
{
    public AuthLogRepository(IDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// 인증 로그를 저장한다.
    /// 
    /// 실패 로그도 반드시 저장해야 운영 중 문제를 추적할 수 있다.
    /// </summary>
    public async Task<long> InsertAsync(AuthLog log)
    {
        const string sql = @"
        INSERT INTO auth_logs
        (
            al_request,
            al_store,
            al_result,
            al_error,
            al_ip,
            al_details,
            al_rdate
        )
        VALUES
        (
            @AlRequest,
            @AlStore,
            @AlResult,
            @AlError,
            @AlIp,
            @AlDetails,
            NOW()
        );

        SELECT LAST_INSERT_ID();
        ";

        return await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<long>(sql, log));
    }

    /// <summary>
    /// 트랜잭션 내부에서 인증 로그를 저장한다.
    /// 
    /// PC 캠 최초 인증처럼 장비 등록과 로그 저장을 함께 처리할 때 사용한다.
    /// </summary>
    public async Task<long> InsertAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        AuthLog log)
    {
        const string sql = @"
        INSERT INTO auth_logs
        (
            al_request,
            al_store,
            al_result,
            al_error,
            al_ip,
            al_details,
            al_rdate
        )
        VALUES
        (
            @AlRequest,
            @AlStore,
            @AlResult,
            @AlError,
            @AlIp,
            @AlDetails,
            NOW()
        );

        SELECT LAST_INSERT_ID();
        ";

        return await connection.ExecuteScalarAsync<long>(sql, log, transaction);
    }
}