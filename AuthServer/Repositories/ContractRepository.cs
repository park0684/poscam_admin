using System.Data;
using Dapper;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Dtos.Store;
using poscam.AuthServer.Models.Dtos.Settlement;

namespace poscam.AuthServer.Repositories;

/// <summary>
/// contracts 테이블 접근 Repository.
/// 
/// 계약 등록, 계약 조회, 매장별 활성 계약 조회를 담당한다.
/// 계약 유효성 판단은 Service에서 처리한다.
/// </summary>
public class ContractRepository : RepositoryBase
{
    public ContractRepository(IDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// 계약 코드로 계약 정보를 조회한다.
    /// 
    /// 캠뷰어 토큰 검증 시 계약의 매장, 캠뷰어 수량,
    /// 계약 상태, 계약 기간을 검증하므로 관련 컬럼을 모두 매핑한다.
    /// </summary>
    public async Task<Contract?> GetByCodeAsync(int contractCode)
    {
        const string sql = @"
        SELECT
            con_code AS ConCode,
            con_store AS ConStore,
            con_type AS ConType,
            con_pcc AS ConPcc,
            con_view AS ConView,
            con_start AS ConStart,
            con_end AS ConEnd,
            con_status AS Status,
            con_rdate AS ConIDate,
            con_udate AS ConUDate
        FROM contracts
        WHERE con_code = @ContractCode
        LIMIT 1;
    ";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<Contract>(
                sql,
                new { ContractCode = contractCode }));
    }

    /// <summary>
    /// 트랜잭션 내부에서 계약 코드로 계약을 조회한다.
    /// </summary>
    public async Task<Contract?> GetByCodeAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        int contractCode)
    {
        const string sql = @"
        SELECT
            con_code  AS ConCode,
            con_store AS ConStore,
            con_partner AS ConPartner,
            con_no    AS ConNo,
            con_type  AS ConType,
            con_pcc   AS ConPcc,
            con_view  AS ConView,
            con_start AS ConStart,
            con_end   AS ConEnd,
            con_status    AS Status,
            con_rdate AS ConRDate,
            con_udate AS ConUDate
        FROM contracts
        WHERE con_code = @ContractCode;
        ";

        return await connection.QueryFirstOrDefaultAsync<Contract>(
            sql,
            new { ContractCode = contractCode },
            transaction);
    }

    /// <summary>
    /// 매장 기준 계약 목록을 조회한다.
    /// 
    /// 캠뷰어 로그인에서는 서비스 계층에서
    /// 계약 상태, 캠뷰어 수량, 계약 기간을 판단하므로
    /// Repository에서는 해당 매장의 계약 정보를 충분히 넓게 조회한다.
    /// </summary>
    public async Task<List<Contract>> GetActiveContractsByStoreAsync(int storeCode)
    {
        const string sql = @"
        SELECT
            con_code AS ConCode,
            con_store AS ConStore,
            con_type AS ConType,
            con_pcc AS ConPcc,
            con_view AS ConView,
            con_start AS ConStart,
            con_end AS ConEnd,
            con_status AS Status,
            con_rdate AS ConIDate,
            con_udate AS ConUDate
        FROM contracts
        WHERE con_store = @StoreCode
        ORDER BY con_start DESC, con_code DESC;
    ";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<Contract>(
                sql,
                new { StoreCode = storeCode }));

        return result.ToList();
    }

    /// <summary>
    /// 계약 번호가 이미 존재하는지 확인한다.
    /// </summary>
    public async Task<bool> ExistsContractNoAsync(string contractNo)
    {
        const string sql = @"SELECT COUNT(1) FROM contracts WHERE con_no = @ContractNo;";

        var count = await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(sql, new { ContractNo = contractNo }));

        return count > 0;
    }

    /// <summary>
    /// 신규 계약을 등록한다.
    /// </summary>
    public async Task<int> InsertAsync(Contract contract)
    {
        const string sql = @"INSERT INTO contracts (con_store,con_no,con_type,con_partner,con_pcc,con_view,con_start,con_end,con_status,con_rdate) " +
        "VALUES(@ConStore, @ConNo, @ConType, @ConPartner, @ConPcc, @ConView, @ConStart, @ConEnd, @Status, NOW());"+
        "SELECT LAST_INSERT_ID();";

        return await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(sql, contract));
    }

    /// <summary>
    /// 신규 계약을 등록한다.
    /// 
    /// 트랜잭션 내부에서 사용할 수 있는 버전이다.
    /// </summary>
    public async Task<int> InsertAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        Contract contract)
    {
        const string sql = @"INSERT INTO contracts (con_store,con_no,con_type,con_partner,con_pcc,con_view,con_start,con_end,con_status,con_rdate) " +
        "VALUES(@ConStore, @ConNo, @ConType, @ConPartner, @ConPcc, @ConView, @ConStart, @ConEnd, @Status, NOW());" +
        "SELECT LAST_INSERT_ID();";

        return await connection.ExecuteScalarAsync<int>(sql, contract, transaction);
    }

    /// <summary>
    /// 특정 매장의 계약 목록을 조회한다.
    /// 
    /// 매장 상세 화면의 계약정보 영역에서 사용한다.
    /// </summary>
    public async Task<List<StoreContractDto>> GetByStoreAsync(int storeCode)
    {
        const string sql = @"SELECT con_code  AS ContractCode, con_store AS StoreCode,con_no AS ContractNo, con_type  AS ContractType, con_pcc   AS PccamCount, con_view  AS ViewerCount,con_start AS StartDate,"+
        "con_end   AS EndDate, con_status AS Status, con_rdate AS RegisteredAt, con_udate AS UpdatedAt FROM contracts WHERE con_store = @StoreCode ORDER BY con_start DESC, con_code DESC;";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<StoreContractDto>(
                sql,
                new { StoreCode = storeCode }));

        return result.ToList();
    }

    /// <summary>
    /// 계약 코드 기준으로 매장 상세 화면용 계약 정보를 조회한다.
    /// </summary>
    public async Task<StoreContractDto?> GetDetailDtoAsync(int contractCode)
    {
        const string sql = @"
SELECT
    con_code  AS ContractCode,
    con_store AS StoreCode,
    con_no    AS ContractNo,
    con_type  AS ContractType,
    con_pcc   AS PccamCount,
    con_view  AS ViewerCount,
    con_start AS StartDate,
    con_end   AS EndDate,
    con_status    AS Status,
    con_rdate AS RegisteredAt,
    con_udate AS UpdatedAt
FROM contracts
WHERE con_code = @ContractCode;
";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<StoreContractDto>(
                sql,
                new { ContractCode = contractCode }));
    }

    /// <summary>
    /// 계약 정보를 수정한다.
    /// 
    /// 계약번호와 매장 코드는 수정하지 않는다.
    /// 계약 유형, 허용 수량, 기간, 상태만 수정한다.
    /// </summary>
    public async Task<int> UpdateAsync(Contract contract)
    {
        const string sql = @"
        UPDATE contracts
        SET
            con_type = @ConType,
            con_pcc = @ConPcc,
            con_view = @ConView,
            con_start = @ConStart,
            con_end = @ConEnd,
            con_status = @Status,
            con_udate = NOW()
        WHERE con_code = @ConCode;
        ";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(sql, contract));
    }

    /// <summary>
    /// ContractRepository 정산용 확장 메서드 예시.
    ///
    /// 아래 메서드는 기존 ContractRepository.cs 내부에 추가해서 사용한다.
    /// 실제 contracts/stores/partners 컬럼명은 현재 DB 스키마에 맞게 조정해야 한다.
    /// 특히 PC캠 수량, 캠뷰어 수량, 계약 파트너 컬럼명은 프로젝트의 실제 컬럼명을 우선한다.
    /// </summary>
    /// <summary>
    /// 청구월 기준 정산 대상 계약 목록을 조회한다.
    ///
    /// 정산 정책:
    /// - PC캠과 캠뷰어 모두 계약서 등록 수량 기준으로 청구한다.
    /// - 파트너사별 단가 정책을 적용해야 하므로 PartnerCode가 반드시 필요하다.
    /// - 계약 상태가 정상인 계약만 대상으로 한다.
    /// - 청구월 말일 기준으로 유효한 계약을 대상으로 한다.
    ///
    /// billMonth 예:
    /// - 202605
    ///
    /// 청구월 판단:
    /// - billMonth의 1일과 말일을 계산한다.
    /// - 계약 시작일이 월 말일보다 작거나 같아야 한다.
    /// - 계약 종료일이 없거나 청구월 1일보다 크거나 같아야 한다.
    /// </summary>
    /// <param name="billMonth">청구월. 예: 202605</param>
    /// <param name="partnerCode">특정 파트너사만 조회할 경우 사용. null이면 전체.</param>
    /// <returns>정산 대상 계약 목록</returns>
    public async Task<List<BillingTargetContractDto>> GetBillingTargetContractsAsync(
        int billMonth,
        int? partnerCode = null)
    {
        var firstDay = new DateTime(billMonth / 100, billMonth % 100, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);

        /*
         * 중요:
         * 아래 SQL의 컬럼명은 현재 프로젝트 스키마에 맞춰 반드시 확인해야 한다.
         *
         * 특히 확인할 컬럼:
         * - 계약 코드: c.con_code
         * - 계약번호: c.con_no
         * - 매장 코드: c.con_store 
         * - 파트너사 코드: c.con_partnuer
         * - PC캠 계약 수량: c.con_pcc
         * - 캠뷰어 계약 수량: c.con_view
         * - 계약 상태: c.con_status 등
         * - 계약 시작/종료일: c.con_start, c.con_end 등
         */
        const string sql = @"
SELECT
    c.con_code AS ContractCode,
    c.con_no AS ContractNo,

    c.con_store AS StoreCode,
    s.store_name AS StoreName,

    c.con_partner AS PartnerCode,
    p.partner_name AS PartnerName,

    c.con_pcc AS PccamCount,
    c.con_view AS ViewerCount,

    c.con_status AS ContractStatus,
    c.con_start AS ContractStartDate,
    c.con_end AS ContractEndDate
FROM contracts c
INNER JOIN stores s
    ON c.con_store = s.store_code
INNER JOIN partners p
    ON c.con_partner = p.partner_code
WHERE c.con_status = 1
  AND (@PartnerCode IS NULL OR c.con_partner = @PartnerCode)
  AND (c.con_start IS NULL OR c.con_start <= @LastDay)
  AND (c.con_end IS NULL OR c.con_end >= @FirstDay)
  AND (COALESCE(c.con_pcc, 0) > 0 OR COALESCE(c.con_view, 0) > 0)
ORDER BY
    p.partner_name ASC,
    s.store_name ASC,
    c.con_code ASC;
";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<BillingTargetContractDto>(
                sql,
                new
                {
                    PartnerCode = partnerCode,
                    FirstDay = firstDay,
                    LastDay = lastDay
                }));

        return result.ToList();
    }
}