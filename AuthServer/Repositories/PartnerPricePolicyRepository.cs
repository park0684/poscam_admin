using Dapper;
using poscam.AuthServer.Models.Dtos.Settlement;
using poscam.AuthServer.Models.Entities;

namespace poscam.AuthServer.Repositories;

/// <summary>
/// 파트너사 단가 정책 Repository.
///
/// DB 테이블: partner_price_policy
///
/// 역할:
/// - 파트너사별 PC캠 월 단가 조회
/// - 파트너사별 캠뷰어 월 단가 조회
/// - 적용월 기준 유효 단가 조회
/// - 단가 정책 신규 등록/수정
/// - 단가 정책 사용중지 처리
///
/// 정산 생성 시 반드시 이 Repository를 통해
/// 해당 청구월에 유효한 파트너사 단가를 조회해야 한다.
/// </summary>
public class PartnerPricePolicyRepository : RepositoryBase
{
    public PartnerPricePolicyRepository(IDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <summary>
    /// 파트너사 단가 정책 목록을 조회한다.
    ///
    /// partnerCode가 null이면 전체 파트너사 단가 정책을 조회한다.
    /// partnerCode가 있으면 해당 파트너사의 단가 정책만 조회한다.
    ///
    /// 관리자 단가관리 화면에서 사용한다.
    /// </summary>
    /// <param name="partnerCode">파트너사 코드. null이면 전체 조회.</param>
    /// <returns>파트너사 단가 정책 목록</returns>
    public async Task<List<PartnerPricePolicyDto>> GetListAsync(int? partnerCode = null)
    {
        const string sql = @"
SELECT
    ppp.ppp_code AS PppCode,
    ppp.ppp_partner AS PartnerCode,
    p.partner_name AS PartnerName,

    ppp.ppp_pccam_price AS PppPccamPrice,
    ppp.ppp_viewer_price AS PppViewerPrice,

    ppp.ppp_start_month AS PppStartMonth,
    ppp.ppp_end_month AS PppEndMonth,

    ppp.ppp_status AS PppStatus,
    ppp.ppp_memo AS PppMemo,

    ppp.ppp_rdate AS PppRdate,
    ppp.ppp_udate AS PppUdate
FROM partner_price_policy ppp
LEFT JOIN partners p
    ON ppp.ppp_partner = p.partner_code
WHERE (@PartnerCode IS NULL OR ppp.ppp_partner = @PartnerCode)
ORDER BY
    p.partner_name ASC,
    ppp.ppp_start_month DESC,
    ppp.ppp_code DESC;
";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<PartnerPricePolicyDto>(
                sql,
                new
                {
                    PartnerCode = partnerCode
                }));

        return result.ToList();
    }

    /// <summary>
    /// 단가 정책 코드를 기준으로 단가 정책을 조회한다.
    ///
    /// 단가 수정 화면에서 기존 데이터를 불러올 때 사용한다.
    /// </summary>
    /// <param name="pppCode">단가 정책 코드</param>
    /// <returns>파트너 단가 정책 Entity</returns>
    public async Task<PartnerPricePolicy?> GetByCodeAsync(int pppCode)
    {
        const string sql = @"
SELECT
    ppp_code AS PppCode,
    ppp_partner AS PartnerCode,
    ppp_pccam_price AS PppPccamPrice,
    ppp_viewer_price AS PppViewerPrice,
    ppp_start_month AS PppStartMonth,
    ppp_end_month AS PppEndMonth,
    ppp_status AS PppStatus,
    ppp_memo AS PppMemo,
    ppp_rdate AS PppRdate,
    ppp_udate AS PppUdate
FROM partner_price_policy
WHERE ppp_code = @PppCode;
";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<PartnerPricePolicy>(
                sql,
                new
                {
                    PppCode = pppCode
                }));
    }

    /// <summary>
    /// 특정 파트너사와 청구월 기준으로 유효한 단가 정책을 조회한다.
    ///
    /// 정산 생성 시 가장 중요한 조회 메서드다.
    ///
    /// 적용 조건:
    /// - ppp_status = 1
    /// - ppp_start_month <= billMonth
    /// - ppp_end_month IS NULL 또는 ppp_end_month >= billMonth
    ///
    /// 동일 조건의 정책이 여러 개라면 적용 시작월이 가장 최근인 정책을 사용한다.
    /// </summary>
    /// <param name="partnerCode">파트너사 코드</param>
    /// <param name="billMonth">청구월. 예: 202605</param>
    /// <returns>해당 월에 적용할 단가 정책</returns>
    public async Task<PartnerPricePolicy?> GetActivePolicyAsync(
        int partnerCode,
        int billMonth)
    {
        const string sql = @"
SELECT
    ppp_code AS PppCode,
    ppp_partner AS PartnerCode,
    ppp_pccam_price AS PppPccamPrice,
    ppp_viewer_price AS PppViewerPrice,
    ppp_start_month AS PppStartMonth,
    ppp_end_month AS PppEndMonth,
    ppp_status AS PppStatus,
    ppp_memo AS PppMemo,
    ppp_rdate AS PppRdate,
    ppp_udate AS PppUdate
FROM partner_price_policy
WHERE ppp_partner = @PartnerCode
  AND ppp_status = 1
  AND ppp_start_month <= @BillMonth
  AND (ppp_end_month IS NULL OR ppp_end_month >= @BillMonth)
ORDER BY ppp_start_month DESC, ppp_code DESC
LIMIT 1;
";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<PartnerPricePolicy>(
                sql,
                new
                {
                    PartnerCode = partnerCode,
                    BillMonth = billMonth
                }));
    }

    /// <summary>
    /// 특정 파트너사에 청구월 기준 유효 단가가 존재하는지 확인한다.
    ///
    /// 월별 청구자료 생성 전에 단가 누락 여부를 검사할 때 사용할 수 있다.
    /// </summary>
    /// <param name="partnerCode">파트너사 코드</param>
    /// <param name="billMonth">청구월</param>
    /// <returns>유효 단가 존재 여부</returns>
    public async Task<bool> ExistsActivePolicyAsync(
        int partnerCode,
        int billMonth)
    {
        const string sql = @"
SELECT COUNT(1)
FROM partner_price_policy
WHERE ppp_partner = @PartnerCode
  AND ppp_status = 1
  AND ppp_start_month <= @BillMonth
  AND (ppp_end_month IS NULL OR ppp_end_month >= @BillMonth);
";

        var count = await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    PartnerCode = partnerCode,
                    BillMonth = billMonth
                }));

        return count > 0;
    }

    /// <summary>
    /// 단가 정책을 신규 등록한다.
    ///
    /// ppp_code는 AUTO_INCREMENT로 생성되며,
    /// 생성된 ppp_code를 반환한다.
    /// </summary>
    /// <param name="policy">등록할 단가 정책 Entity</param>
    /// <returns>생성된 단가 정책 코드</returns>
    public async Task<int> InsertAsync(PartnerPricePolicy policy)
    {
        const string sql = @"
INSERT INTO partner_price_policy
(
    ppp_partner,
    ppp_pccam_price,
    ppp_viewer_price,
    ppp_start_month,
    ppp_end_month,
    ppp_status,
    ppp_memo,
    ppp_rdate,
    ppp_udate
)
VALUES
(
    @PartnerCode,
    @PppPccamPrice,
    @PppViewerPrice,
    @PppStartMonth,
    @PppEndMonth,
    @PppStatus,
    @PppMemo,
    NOW(),
    NULL
);

SELECT LAST_INSERT_ID();
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    policy.PartnerCode,
                    policy.PppPccamPrice,
                    policy.PppViewerPrice,
                    policy.PppStartMonth,
                    policy.PppEndMonth,
                    policy.PppStatus,
                    policy.PppMemo
                }));
    }

    /// <summary>
    /// 단가 정책을 수정한다.
    ///
    /// 이미 생성된 청구자료는 contract_billing에 단가 스냅샷이 저장되어 있으므로
    /// 이 단가 정책을 수정해도 과거 청구자료에는 영향을 주지 않는다.
    /// </summary>
    /// <param name="policy">수정할 단가 정책 Entity</param>
    /// <returns>영향받은 행 수</returns>
    public async Task<int> UpdateAsync(PartnerPricePolicy policy)
    {
        const string sql = @"
UPDATE partner_price_policy
SET
    ppp_partner = @PartnerCode,
    ppp_pccam_price = @PppPccamPrice,
    ppp_viewer_price = @PppViewerPrice,
    ppp_start_month = @PppStartMonth,
    ppp_end_month = @PppEndMonth,
    ppp_status = @PppStatus,
    ppp_memo = @PppMemo,
    ppp_udate = NOW()
WHERE ppp_code = @PppCode;
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(
                sql,
                new
                {
                    policy.PppCode,
                    policy.PartnerCode,
                    policy.PppPccamPrice,
                    policy.PppViewerPrice,
                    policy.PppStartMonth,
                    policy.PppEndMonth,
                    policy.PppStatus,
                    policy.PppMemo
                }));
    }

    /// <summary>
    /// 단가 정책 사용 여부를 변경한다.
    ///
    /// 삭제 대신 상태값을 변경하여 이력을 보존한다.
    /// </summary>
    /// <param name="pppCode">단가 정책 코드</param>
    /// <param name="status">1=사용, 0=미사용</param>
    /// <returns>영향받은 행 수</returns>
    public async Task<int> UpdateStatusAsync(
        int pppCode,
        int status)
    {
        const string sql = @"
UPDATE partner_price_policy
SET
    ppp_status = @Status,
    ppp_udate = NOW()
WHERE ppp_code = @PppCode;
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(
                sql,
                new
                {
                    PppCode = pppCode,
                    Status = status
                }));
    }

    /// <summary>
    /// 단가 정책 기간 중복 여부를 확인한다.
    ///
    /// 같은 파트너사에 대해 사용 상태의 단가 정책 기간이 겹치면
    /// 정산 생성 시 어떤 단가를 써야 하는지 모호해진다.
    ///
    /// 따라서 단가 저장 전 Service에서 이 메서드로 중복 여부를 검사하는 것이 좋다.
    ///
    /// 비교 기준:
    /// - 기존 시작월 <= 신규 종료월
    /// - 기존 종료월이 NULL이거나 기존 종료월 >= 신규 시작월
    ///
    /// 신규 종료월이 NULL이면 매우 큰 값으로 간주한다.
    /// </summary>
    /// <param name="partnerCode">파트너사 코드</param>
    /// <param name="startMonth">신규/수정 시작월</param>
    /// <param name="endMonth">신규/수정 종료월</param>
    /// <param name="excludePppCode">수정 시 자기 자신 제외용 ppp_code</param>
    /// <returns>중복 여부</returns>
    public async Task<bool> ExistsOverlappedPeriodAsync(
        int partnerCode,
        int startMonth,
        int? endMonth,
        int? excludePppCode = null)
    {
        const string sql = @"
SELECT COUNT(1)
FROM partner_price_policy
WHERE ppp_partner = @PartnerCode
  AND ppp_status = 1
  AND (@ExcludePppCode IS NULL OR ppp_code <> @ExcludePppCode)
  AND ppp_start_month <= COALESCE(@EndMonth, 999912)
  AND COALESCE(ppp_end_month, 999912) >= @StartMonth;
";

        var count = await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    PartnerCode = partnerCode,
                    StartMonth = startMonth,
                    EndMonth = endMonth,
                    ExcludePppCode = excludePppCode
                }));

        return count > 0;
    }
}
