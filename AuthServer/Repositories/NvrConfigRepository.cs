using System.Data;
using Dapper;
using poscam.AuthServer.Models.Entities;

namespace poscam.AuthServer.Repositories;

/// <summary>
/// nvr_configs 테이블 접근 Repository.
///
/// 매장별 NVR 접속 설정 저장/조회 기능을 담당한다.
/// 다중 NVR 구조에서는 (nvr_store, nvr_no)가 NVR 식별 기준이다.
/// </summary>
public class NvrConfigRepository : RepositoryBase
{
    public NvrConfigRepository(IDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// 매장 코드 기준으로 NVR 설정 목록을 조회한다.
    /// </summary>
    public async Task<List<NvrConfig>> GetListByStoreAsync(int storeCode)
    {
        const string sql = @"
        SELECT
            nvr_store     AS NvrStore,
            nvr_no        AS NvrNo,
            nvr_provider  AS NvrProvider,
            nvr_id        AS NvrId,
            nvr_password  AS NvrPassword,
            nvr_ip        AS NvrIp,
            nvr_port      AS NvrPort,
            nvr_rtsp_port AS NvrRtspPort,
            nvr_channels  AS NvrChannels,
            nvr_version   AS NvrVersion,
            nvr_rdate     AS NvrRDate,
            nvr_udate     AS NvrUDate
        FROM nvr_configs
        WHERE nvr_store = @StoreCode
        ORDER BY nvr_no ASC;
        ";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<NvrConfig>(
                sql,
                new { StoreCode = storeCode }));

        return result.ToList();
    }

    /// <summary>
    /// 기존 단일 NVR 호출부 호환용 조회.
    /// NVR 번호가 가장 작은 첫 설정을 반환한다.
    /// 신규 다중 NVR 로직에서는 GetListByStoreAsync를 사용한다.
    /// </summary>
    public async Task<NvrConfig?> GetByStoreAsync(int storeCode)
    {
        var result = await GetListByStoreAsync(storeCode);
        return result.FirstOrDefault();
    }

    /// <summary>
    /// NVR 설정을 저장한다.
    /// (nvr_store, nvr_no)가 이미 존재하면 UPDATE, 없으면 INSERT한다.
    /// </summary>
    public async Task<int> UpsertAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        NvrConfig config)
    {
        const string sql = @"
        INSERT INTO nvr_configs
        (
            nvr_store,
            nvr_no,
            nvr_provider,
            nvr_id,
            nvr_password,
            nvr_ip,
            nvr_port,
            nvr_rtsp_port,
            nvr_channels,
            nvr_version,
            nvr_rdate
        )
        VALUES
        (
            @NvrStore,
            @NvrNo,
            @NvrProvider,
            @NvrId,
            @NvrPassword,
            @NvrIp,
            @NvrPort,
            @NvrRtspPort,
            @NvrChannels,
            @NvrVersion,
            NOW()
        )
        ON DUPLICATE KEY UPDATE
            nvr_provider  = VALUES(nvr_provider),
            nvr_id        = VALUES(nvr_id),
            nvr_password  = VALUES(nvr_password),
            nvr_ip        = VALUES(nvr_ip),
            nvr_port      = VALUES(nvr_port),
            nvr_rtsp_port = VALUES(nvr_rtsp_port),
            nvr_channels  = VALUES(nvr_channels),
            nvr_version   = VALUES(nvr_version),
            nvr_udate     = NOW();
        ";

        return await connection.ExecuteAsync(sql, config, transaction);
    }

    /// <summary>
    /// 특정 매장의 NVR 설정을 모두 삭제한다.
    /// Schema 2 전체 설정 동기화 트랜잭션에서 채널 삭제 후 호출한다.
    /// </summary>
    public async Task<int> DeleteByStoreAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        int storeCode)
    {
        const string sql = @"
        DELETE FROM nvr_configs
        WHERE nvr_store = @StoreCode;
        ";

        return await connection.ExecuteAsync(
            sql,
            new { StoreCode = storeCode },
            transaction);
    }
}
