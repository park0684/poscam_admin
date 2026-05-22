using Dapper;
using poscam.AuthServer.Models.Dtos.Partner;
using poscam.AuthServer.Models.Entities;

namespace poscam.AuthServer.Repositories;

/// <summary>
/// partners 테이블 접근 Repository.
/// 
/// 파트너사 등록, 수정, 목록 조회, 상세 조회를 담당한다.
/// 파트너사의 역할은 이 테이블에서 고정하지 않고,
/// 매장 연결 시 assignment_role로 관리한다.
/// </summary>
public class PartnerRepository : RepositoryBase
{
    public PartnerRepository(IDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// 파트너사 코드로 상세 정보를 조회한다.
    /// </summary>
    public async Task<Partner?> GetByCodeAsync(int partnerCode)
    {
        const string sql = @"
        SELECT
            partner_code       AS PartnerCode,
            partner_name       AS PartnerName,
            partner_biznum     AS PartnerBizNum,
            partner_owner_name AS PartnerOwnerName,
            partner_tel        AS PartnerTel,
            partner_email      AS PartnerEmail,
            partner_zipcode    AS PartnerZipcode,
            partner_address1   AS PartnerAddress1,
            partner_address2   AS PartnerAddress2,
            partner_memo       AS PartnerMemo,
            partner_status     AS PartnerStatus,
            partner_rdate      AS PartnerRDate,
            partner_udate      AS PartnerUDate
        FROM partners
        WHERE partner_code = @PartnerCode;
        ";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<Partner>(
                sql,
                new { PartnerCode = partnerCode }));
    }

    /// <summary>
    /// 파트너사 목록을 조회한다.
    /// 관리자 화면의 파트너사 목록에서 사용한다.
    /// </summary>
    public async Task<List<PartnerListItemDto>> GetListAsync()
    {
        const string sql = @"
        SELECT
            partner_code       AS PartnerCode,
            partner_name       AS PartnerName,
            partner_biznum     AS PartnerBizNum,
            partner_owner_name AS PartnerOwnerName,
            partner_tel        AS PartnerTel,
            partner_email      AS PartnerEmail,
            partner_status     AS PartnerStatus,
            partner_rdate      AS RegisteredAt
        FROM partners
        ORDER BY partner_rdate DESC, partner_code DESC;
        ";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<PartnerListItemDto>(sql));

        return result.ToList();
    }

    /// <summary>
    /// 파트너사 상세 DTO를 조회한다.
    /// </summary>
    public async Task<PartnerDetailDto?> GetDetailAsync(int partnerCode)
    {
        const string sql = @"
        SELECT
            partner_code       AS PartnerCode,
            partner_name       AS PartnerName,
            partner_biznum     AS PartnerBizNum,
            partner_owner_name AS PartnerOwnerName,
            partner_tel        AS PartnerTel,
            partner_email      AS PartnerEmail,
            partner_zipcode    AS PartnerZipcode,
            partner_address1   AS PartnerAddress1,
            partner_address2   AS PartnerAddress2,
            partner_memo       AS PartnerMemo,
            partner_status     AS PartnerStatus,
            partner_rdate      AS RegisteredAt,
            partner_udate      AS UpdatedAt
        FROM partners
        WHERE partner_code = @PartnerCode;
        ";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<PartnerDetailDto>(
                sql,
                new { PartnerCode = partnerCode }));
    }

    /// <summary>
    /// 신규 파트너사를 등록한다.
    /// </summary>
    public async Task<int> InsertAsync(Partner partner)
    {
        const string sql = @"
        INSERT INTO partners
        (
            partner_name,
            partner_biznum,
            partner_owner_name,
            partner_tel,
            partner_email,
            partner_zipcode,
            partner_address1,
            partner_address2,
            partner_memo,
            partner_status,
            partner_rdate
        )
        VALUES
        (
            @PartnerName,
            @PartnerBizNum,
            @PartnerOwnerName,
            @PartnerTel,
            @PartnerEmail,
            @PartnerZipcode,
            @PartnerAddress1,
            @PartnerAddress2,
            @PartnerMemo,
            @PartnerStatus,
            NOW()
        );

        SELECT LAST_INSERT_ID();
        ";

        return await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(sql, partner));
    }

    /// <summary>
    /// 파트너사 정보를 수정한다.
    /// </summary>
    public async Task<int> UpdateAsync(Partner partner)
    {
        const string sql = @"
        UPDATE partners
        SET
            partner_name = @PartnerName,
            partner_biznum = @PartnerBizNum,
            partner_owner_name = @PartnerOwnerName,
            partner_tel = @PartnerTel,
            partner_email = @PartnerEmail,
            partner_zipcode = @PartnerZipcode,
            partner_address1 = @PartnerAddress1,
            partner_address2 = @PartnerAddress2,
            partner_memo = @PartnerMemo,
            partner_status = @PartnerStatus,
            partner_udate = NOW()
        WHERE partner_code = @PartnerCode;
        ";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(sql, partner));
    }
}