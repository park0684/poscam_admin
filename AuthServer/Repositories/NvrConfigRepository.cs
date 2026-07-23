using System.Data;
using Dapper;
using poscam.AuthServer.Models.Entities;

namespace poscam.AuthServer.Repositories;

/// <summary>
/// nvr_configs 테이블 접근 Repository.
///
/// 매장별 NVR 접속 설정 저장/조회 기능을 담당한다.
/// </summary>
public class NvrConfigRepository : RepositoryBase
{
    public NvrConfigRepository(IDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// 매장 코드 기준으로 NVR 설정을 조회한다.
    /// </summary>
    public async Task<NvrConfig?> GetByStoreAsync(int storeCode)
    {
        const string sql = @"
        SELECT
            nvr_store     AS NvrStore,
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
        WHERE nvr_store = @StoreCode;
        ";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<NvrConfig>(
                sql,
                new { StoreCode = storeCode }));
    }

    /// <summary>
    /// NVR 설정을 저장한다.
    ///
    /// nvr_store가 PRIMARY KEY이므로
    /// 이미 존재하면 UPDATE, 없으면 INSERT한다.
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
}
