using System.Data;
using Dapper;
using poscam.AuthServer.Models.Entities;

namespace poscam.AuthServer.Repositories;

/// <summary>
/// ch_config 테이블 접근 Repository.
/// 
/// POS 번호와 NVR 채널 매핑 정보를 저장/조회한다.
/// </summary>
public class ChannelConfigRepository : RepositoryBase
{
    public ChannelConfigRepository(IDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// 매장 코드 기준으로 채널 매핑 목록을 조회한다.
    /// </summary>
    public async Task<List<ChannelConfig>> GetByStoreAsync(int storeCode)
    {
        const string sql = @"
        SELECT
            chn_store  AS ChnStore,
            chn_pos    AS ChnPos,
            chn_ch     AS ChnCh,
            chn_screen AS ChnScreen,
            chn_rdate  AS ChnRDate,
            chn_udate  AS ChnUDate
        FROM ch_config
        WHERE chn_store = @StoreCode
        ORDER BY chn_pos ASC, chn_screen ASC;
        ";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<ChannelConfig>(
                sql,
                new { StoreCode = storeCode }));

        return result.ToList();
    }

    /// <summary>
    /// 채널 매핑 정보를 저장한다.
    /// 
    /// PRIMARY KEY가 chn_store + chn_pos + chn_screen이므로
    /// 같은 위치의 설정이 이미 있으면 채널 번호만 UPDATE한다.
    /// </summary>
    public async Task<int> UpsertAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        ChannelConfig config)
    {
        const string sql = @"
        INSERT INTO ch_config
        (
            chn_store,
            chn_pos,
            chn_ch,
            chn_screen,
            chn_rdate
        )
        VALUES
        (
            @ChnStore,
            @ChnPos,
            @ChnCh,
            @ChnScreen,
            NOW()
        )
        ON DUPLICATE KEY UPDATE
            chn_ch    = VALUES(chn_ch),
            chn_udate = NOW();
        ";

        return await connection.ExecuteAsync(sql, config, transaction);
    }

    /// <summary>
    /// 특정 매장의 채널 매핑을 모두 삭제한다.
    /// 
    /// 캠뷰어 설정 동기화에서 전체 설정을 다시 업로드할 때 사용할 수 있다.
    /// </summary>
    public async Task<int> DeleteByStoreAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        int storeCode)
    {
        const string sql = @"
        DELETE FROM ch_config
        WHERE chn_store = @StoreCode;
        ";

        return await connection.ExecuteAsync(
            sql,
            new { StoreCode = storeCode },
            transaction);
    }
}