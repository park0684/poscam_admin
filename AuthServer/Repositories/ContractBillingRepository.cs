using System.Data;
using Dapper;
using poscam.AuthServer.Models.Dtos.Settlement;
using poscam.AuthServer.Models.Entities;

namespace poscam.AuthServer.Repositories;

/// <summary>
/// 계약별 월 청구자료 Repository.
///
/// DB 테이블: contract_billing
///
/// 역할:
/// - 월별 계약 청구자료 등록
/// - 월별 계약 청구자료 조회
/// - 파트너사별 월 정산 합산 조회
/// - 청구대기 자료 재생성 전 삭제
/// - 청구/납부 상태 변경
///
/// 주의:
/// - contract_billing은 월별 청구 스냅샷 테이블이다.
/// - 계약 수량, 적용 단가, 청구 금액은 생성 시점에 저장된다.
/// - 이후 계약 정보나 파트너 단가가 바뀌어도 이미 생성된 청구자료는 자동 변경하지 않는다.
/// </summary>
public class ContractBillingRepository : RepositoryBase
{
    public ContractBillingRepository(IDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <summary>
    /// 계약별 월 청구내역 목록을 조회한다.
    ///
    /// 월별 계약 청구내역 화면에서 사용한다.
    /// partnerCode, storeCode, paymentStatus는 선택 조건이다.
    /// </summary>
    /// <param name="billMonth">청구월. 예: 202605</param>
    /// <param name="partnerCode">파트너사 코드. null이면 전체.</param>
    /// <param name="storeCode">매장 코드. null이면 전체.</param>
    /// <param name="paymentStatus">납부 상태. null이면 전체.</param>
    /// <returns>계약별 청구내역 목록</returns>
    public async Task<List<ContractBillingListItemDto>> GetListAsync(
        int billMonth,
        int? partnerCode = null,
        int? storeCode = null,
        int? paymentStatus = null)
    {
        const string sql = @"
SELECT
    cb.bill_code AS BillCode,
    cb.bill_month AS BillMonth,

    cb.partner_code AS PartnerCode,
    p.partner_name AS PartnerName,

    cb.store_code AS StoreCode,
    s.store_name AS StoreName,

    cb.contract_code AS ContractCode,
    cb.contract_no AS ContractNo,

    cb.bill_pccam_count AS BillPccamCount,
    cb.bill_viewer_count AS BillViewerCount,

    cb.bill_pccam_unit_price AS BillPccamUnitPrice,
    cb.bill_viewer_unit_price AS BillViewerUnitPrice,

    cb.bill_pccam_amount AS BillPccamAmount,
    cb.bill_viewer_amount AS BillViewerAmount,
    cb.bill_total_amount AS BillTotalAmount,

    cb.bill_status AS BillStatus,
    cb.payment_status AS PaymentStatus,

    cb.bill_memo AS BillMemo,
    cb.bill_rdate AS BillRdate,
    cb.bill_udate AS BillUdate
FROM contract_billing cb
LEFT JOIN partners p
    ON cb.partner_code = p.partner_code
LEFT JOIN stores s
    ON cb.store_code = s.store_code
WHERE cb.bill_month = @BillMonth
  AND (@PartnerCode IS NULL OR cb.partner_code = @PartnerCode)
  AND (@StoreCode IS NULL OR cb.store_code = @StoreCode)
  AND (@PaymentStatus IS NULL OR cb.payment_status = @PaymentStatus)
ORDER BY
    p.partner_name ASC,
    s.store_name ASC,
    cb.contract_code ASC;
";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<ContractBillingListItemDto>(
                sql,
                new
                {
                    BillMonth = billMonth,
                    PartnerCode = partnerCode,
                    StoreCode = storeCode,
                    PaymentStatus = paymentStatus
                }));

        return result.ToList();
    }

    /// <summary>
    /// 청구 코드 기준으로 계약별 청구자료를 조회한다.
    ///
    /// 상세 조회 또는 상태 변경 전 검증에 사용한다.
    /// </summary>
    /// <param name="billCode">청구 코드</param>
    /// <returns>계약별 청구자료 Entity</returns>
    public async Task<ContractBilling?> GetByCodeAsync(int billCode)
    {
        const string sql = @"
SELECT
    bill_code AS BillCode,
    bill_month AS BillMonth,
    partner_code AS PartnerCode,
    store_code AS StoreCode,
    contract_code AS ContractCode,
    contract_no AS ContractNo,

    bill_pccam_count AS BillPccamCount,
    bill_viewer_count AS BillViewerCount,

    bill_pccam_unit_price AS BillPccamUnitPrice,
    bill_viewer_unit_price AS BillViewerUnitPrice,

    bill_pccam_amount AS BillPccamAmount,
    bill_viewer_amount AS BillViewerAmount,
    bill_total_amount AS BillTotalAmount,

    bill_status AS BillStatus,
    payment_status AS PaymentStatus,

    bill_memo AS BillMemo,
    bill_rdate AS BillRdate,
    bill_udate AS BillUdate
FROM contract_billing
WHERE bill_code = @BillCode;
";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<ContractBilling>(
                sql,
                new
                {
                    BillCode = billCode
                }));
    }

    /// <summary>
    /// 특정 청구월 + 계약 코드 기준 청구자료 존재 여부를 확인한다.
    ///
    /// 같은 계약이 같은 월에 중복 청구자료로 생성되는 것을 방지한다.
    /// DB에도 UNIQUE KEY uq_contract_billing_month_contract가 있으므로
    /// 이 메서드는 사전 검증 용도다.
    /// </summary>
    /// <param name="billMonth">청구월</param>
    /// <param name="contractCode">계약 코드</param>
    /// <returns>존재 여부</returns>
    public async Task<bool> ExistsAsync(
        int billMonth,
        int contractCode)
    {
        const string sql = @"
SELECT COUNT(1)
FROM contract_billing
WHERE bill_month = @BillMonth
  AND contract_code = @ContractCode;
";

        var count = await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    BillMonth = billMonth,
                    ContractCode = contractCode
                }));

        return count > 0;
    }

    /// <summary>
    /// 특정 청구월에 생성된 청구자료 개수를 조회한다.
    ///
    /// 정산 생성 전 이미 생성된 자료가 있는지 확인할 때 사용한다.
    /// </summary>
    /// <param name="billMonth">청구월</param>
    /// <param name="partnerCode">파트너사 코드. null이면 전체.</param>
    /// <returns>청구자료 개수</returns>
    public async Task<int> CountByMonthAsync(
        int billMonth,
        int? partnerCode = null)
    {
        const string sql = @"
SELECT COUNT(1)
FROM contract_billing
WHERE bill_month = @BillMonth
  AND (@PartnerCode IS NULL OR partner_code = @PartnerCode);
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    BillMonth = billMonth,
                    PartnerCode = partnerCode
                }));
    }

    /// <summary>
    /// 청구대기 상태의 월별 청구자료를 삭제한다.
    ///
    /// 월별 청구자료 재생성 시 사용한다.
    /// 단, 청구확정 또는 납부처리된 자료는 삭제하지 않는다.
    ///
    /// 삭제 조건:
    /// - bill_status = 1 청구대기
    /// - payment_status = 0 미처리
    ///
    /// 이 조건을 벗어나는 자료는 운영상 확정 또는 처리된 자료로 보고 유지한다.
    /// </summary>
    /// <param name="billMonth">청구월</param>
    /// <param name="partnerCode">파트너사 코드. null이면 전체.</param>
    /// <returns>삭제된 행 수</returns>
    public async Task<int> DeletePendingAsync(
        int billMonth,
        int? partnerCode = null)
    {
        const string sql = @"
DELETE FROM contract_billing
WHERE bill_month = @BillMonth
  AND (@PartnerCode IS NULL OR partner_code = @PartnerCode)
  AND bill_status = 1
  AND payment_status = 0;
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(
                sql,
                new
                {
                    BillMonth = billMonth,
                    PartnerCode = partnerCode
                }));
    }

    /// <summary>
    /// 재생성할 수 없는 청구자료 개수를 조회한다.
    ///
    /// bill_status가 청구대기가 아니거나 payment_status가 미처리가 아닌 자료는
    /// 삭제 후 재생성하면 안 된다.
    ///
    /// 예:
    /// - 청구확정
    /// - 부분납부
    /// - 납부완료
    /// - 보류
    /// </summary>
    /// <param name="billMonth">청구월</param>
    /// <param name="partnerCode">파트너사 코드. null이면 전체.</param>
    /// <returns>재생성 불가 자료 개수</returns>
    public async Task<int> CountLockedBillingAsync(
        int billMonth,
        int? partnerCode = null)
    {
        const string sql = @"
SELECT COUNT(1)
FROM contract_billing
WHERE bill_month = @BillMonth
  AND (@PartnerCode IS NULL OR partner_code = @PartnerCode)
  AND NOT (bill_status = 1 AND payment_status = 0);
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    BillMonth = billMonth,
                    PartnerCode = partnerCode
                }));
    }

    /// <summary>
    /// 계약별 월 청구자료를 신규 등록한다.
    ///
    /// 정산 생성 시 계약 1건당 1개의 contract_billing row를 생성한다.
    /// </summary>
    /// <param name="billing">등록할 청구자료 Entity</param>
    /// <returns>생성된 bill_code</returns>
    public async Task<int> InsertAsync(ContractBilling billing)
    {
        const string sql = @"
INSERT INTO contract_billing
(
    bill_month,
    partner_code,
    store_code,
    contract_code,
    contract_no,

    bill_pccam_count,
    bill_viewer_count,

    bill_pccam_unit_price,
    bill_viewer_unit_price,

    bill_pccam_amount,
    bill_viewer_amount,
    bill_total_amount,

    bill_status,
    payment_status,

    bill_memo,
    bill_rdate,
    bill_udate
)
VALUES
(
    @BillMonth,
    @PartnerCode,
    @StoreCode,
    @ContractCode,
    @ContractNo,

    @BillPccamCount,
    @BillViewerCount,

    @BillPccamUnitPrice,
    @BillViewerUnitPrice,

    @BillPccamAmount,
    @BillViewerAmount,
    @BillTotalAmount,

    @BillStatus,
    @PaymentStatus,

    @BillMemo,
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
                    billing.BillMonth,
                    billing.PartnerCode,
                    billing.StoreCode,
                    billing.ContractCode,
                    billing.ContractNo,

                    billing.BillPccamCount,
                    billing.BillViewerCount,

                    billing.BillPccamUnitPrice,
                    billing.BillViewerUnitPrice,

                    billing.BillPccamAmount,
                    billing.BillViewerAmount,
                    billing.BillTotalAmount,

                    billing.BillStatus,
                    billing.PaymentStatus,
                    billing.BillMemo
                }));
    }

    /// <summary>
    /// 계약별 월 청구자료를 여러 건 등록한다.
    ///
    /// 대량 생성 시 사용한다.
    /// 현재 RepositoryBase 구조를 고려해 하나의 Connection과 Transaction 안에서 처리한다.
    ///
    /// 반환값은 등록된 행 수다.
    /// </summary>
    /// <param name="billings">등록할 청구자료 목록</param>
    /// <returns>등록된 행 수</returns>
    public async Task<int> InsertManyAsync(IEnumerable<ContractBilling> billings)
    {
        var list = billings.ToList();

        if (list.Count == 0)
        {
            return 0;
        }

        const string sql = @"
INSERT INTO contract_billing
(
    bill_month,
    partner_code,
    store_code,
    contract_code,
    contract_no,

    bill_pccam_count,
    bill_viewer_count,

    bill_pccam_unit_price,
    bill_viewer_unit_price,

    bill_pccam_amount,
    bill_viewer_amount,
    bill_total_amount,

    bill_status,
    payment_status,

    bill_memo,
    bill_rdate,
    bill_udate
)
VALUES
(
    @BillMonth,
    @PartnerCode,
    @StoreCode,
    @ContractCode,
    @ContractNo,

    @BillPccamCount,
    @BillViewerCount,

    @BillPccamUnitPrice,
    @BillViewerUnitPrice,

    @BillPccamAmount,
    @BillViewerAmount,
    @BillTotalAmount,

    @BillStatus,
    @PaymentStatus,

    @BillMemo,
    NOW(),
    NULL
);
";

        return await WithConnectionAsync(async conn =>
        {
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            using var tran = conn.BeginTransaction();

            try
            {
                var affected = await conn.ExecuteAsync(sql, list, tran);
                tran.Commit();
                return affected;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        });
    }

    /// <summary>
    /// 계약별 청구 상태를 변경한다.
    ///
    /// 예:
    /// - 청구대기에서 청구확정으로 변경
    /// - 잘못 생성된 자료를 취소 처리
    /// </summary>
    /// <param name="billCode">청구 코드</param>
    /// <param name="billStatus">변경할 청구 상태</param>
    /// <param name="memo">메모</param>
    /// <returns>영향받은 행 수</returns>
    public async Task<int> UpdateBillStatusAsync(
        int billCode,
        int billStatus,
        string? memo = null)
    {
        const string sql = @"
UPDATE contract_billing
SET
    bill_status = @BillStatus,
    bill_memo = COALESCE(@Memo, bill_memo),
    bill_udate = NOW()
WHERE bill_code = @BillCode;
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(
                sql,
                new
                {
                    BillCode = billCode,
                    BillStatus = billStatus,
                    Memo = memo
                }));
    }

    /// <summary>
    /// 특정 청구월 + 파트너사 기준 계약 청구자료의 납부 상태를 일괄 변경한다.
    ///
    /// 파트너사 월 납부 처리 후 contract_billing.payment_status를 동기화할 때 사용한다.
    /// </summary>
    /// <param name="billMonth">청구월</param>
    /// <param name="partnerCode">파트너사 코드</param>
    /// <param name="paymentStatus">납부 상태</param>
    /// <returns>영향받은 행 수</returns>
    public async Task<int> UpdatePaymentStatusByPartnerMonthAsync(
        int billMonth,
        int partnerCode,
        int paymentStatus)
    {
        const string sql = @"
UPDATE contract_billing
SET
    payment_status = @PaymentStatus,
    bill_udate = NOW()
WHERE bill_month = @BillMonth
  AND partner_code = @PartnerCode
  AND bill_status <> 9;
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(
                sql,
                new
                {
                    BillMonth = billMonth,
                    PartnerCode = partnerCode,
                    PaymentStatus = paymentStatus
                }));
    }

    /// <summary>
    /// 파트너사별 월 정산 합산 목록을 조회한다.
    ///
    /// contract_billing을 bill_month + partner_code 기준으로 합산하고,
    /// billing_payment가 있으면 납부금액/미납금액/납부상태를 함께 조회한다.
    ///
    /// 파트너사별 월 정산 화면에서 사용한다.
    /// </summary>
    /// <param name="billMonth">청구월</param>
    /// <param name="partnerCode">파트너사 코드. null이면 전체.</param>
    /// <returns>파트너사별 월 정산 합산 목록</returns>
    public async Task<List<PartnerMonthlySettlementDto>> GetPartnerMonthlySettlementAsync(
        int billMonth,
        int? partnerCode = null)
    {
        const string sql = @"
SELECT
    cb.bill_month AS BillMonth,
    cb.partner_code AS PartnerCode,
    p.partner_name AS PartnerName,

    COUNT(DISTINCT cb.contract_code) AS ContractCount,

    SUM(cb.bill_pccam_count) AS PccamTotalCount,
    SUM(cb.bill_viewer_count) AS ViewerTotalCount,

    SUM(cb.bill_pccam_amount) AS PccamTotalAmount,
    SUM(cb.bill_viewer_amount) AS ViewerTotalAmount,
    SUM(cb.bill_total_amount) AS TotalAmount,

    COALESCE(bp.pay_amount, 0) AS PaidAmount,
    COALESCE(bp.pay_remain_amount, SUM(cb.bill_total_amount)) AS RemainAmount,
    COALESCE(bp.pay_status, 0) AS PaymentStatus
FROM contract_billing cb
LEFT JOIN partners p
    ON cb.partner_code = p.partner_code
LEFT JOIN billing_payment bp
    ON cb.bill_month = bp.bill_month
   AND cb.partner_code = bp.partner_code
WHERE cb.bill_month = @BillMonth
  AND cb.bill_status <> 9
  AND (@PartnerCode IS NULL OR cb.partner_code = @PartnerCode)
GROUP BY
    cb.bill_month,
    cb.partner_code,
    p.partner_name,
    bp.pay_amount,
    bp.pay_remain_amount,
    bp.pay_status
ORDER BY p.partner_name ASC;
";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<PartnerMonthlySettlementDto>(
                sql,
                new
                {
                    BillMonth = billMonth,
                    PartnerCode = partnerCode
                }));

        return result.ToList();
    }

    /// <summary>
    /// 특정 청구월 + 파트너사 기준 총 청구금액을 조회한다.
    ///
    /// billing_payment 생성/수정 시 청구금액 합계를 계산하는 데 사용한다.
    /// </summary>
    /// <param name="billMonth">청구월</param>
    /// <param name="partnerCode">파트너사 코드</param>
    /// <returns>총 청구금액</returns>
    public async Task<int> GetTotalAmountByPartnerMonthAsync(
        int billMonth,
        int partnerCode)
    {
        const string sql = @"
SELECT COALESCE(SUM(bill_total_amount), 0)
FROM contract_billing
WHERE bill_month = @BillMonth
  AND partner_code = @PartnerCode
  AND bill_status <> 9;
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    BillMonth = billMonth,
                    PartnerCode = partnerCode
                }));
    }

    /// <summary>
    /// 특정 청구월의 파트너사 코드 목록을 조회한다.
    ///
    /// 월별 청구자료 생성 후 billing_payment 초기 자료를 생성할 때 사용한다.
    /// </summary>
    /// <param name="billMonth">청구월</param>
    /// <returns>파트너사 코드 목록</returns>
    public async Task<List<int>> GetPartnerCodesByBillMonthAsync(int billMonth)
    {
        const string sql = @"
SELECT DISTINCT partner_code
FROM contract_billing
WHERE bill_month = @BillMonth
  AND bill_status <> 9
ORDER BY partner_code ASC;
";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<int>(
                sql,
                new
                {
                    BillMonth = billMonth
                }));

        return result.ToList();
    }

    /// <summary>
    /// 청구월 + 파트너사 조건으로 특정 청구상태의 자료 개수를 조회한다.
    ///
    /// billStatus 예:
    /// 1=청구대기, 2=청구확정, 9=취소
    /// </summary>
    public async Task<int> CountByBillStatusAsync(
        int billMonth,
        int? partnerCode,
        int billStatus)
    {
        const string sql = @"
SELECT COUNT(1)
FROM contract_billing
WHERE bill_month = @BillMonth
  AND (@PartnerCode IS NULL OR partner_code = @PartnerCode)
  AND bill_status = @BillStatus;
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    BillMonth = billMonth,
                    PartnerCode = partnerCode,
                    BillStatus = billStatus
                }));
    }

    /// <summary>
    /// 청구대기 상태의 계약 청구자료를 청구확정 상태로 일괄 변경한다.
    ///
    /// 변경 조건:
    /// - bill_status = 1 청구대기
    /// - payment_status = 0 미처리
    ///
    /// 납부 처리가 이미 진행된 자료는 확정 대상에서 제외한다.
    /// </summary>
    public async Task<int> ConfirmPendingChargesAsync(
        int billMonth,
        int? partnerCode,
        string? memo)
    {
        const string sql = @"
UPDATE contract_billing
SET
    bill_status = 2,
    bill_memo = COALESCE(@Memo, bill_memo),
    bill_udate = NOW()
WHERE bill_month = @BillMonth
  AND (@PartnerCode IS NULL OR partner_code = @PartnerCode)
  AND bill_status = 1
  AND payment_status = 0;
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(
                sql,
                new
                {
                    BillMonth = billMonth,
                    PartnerCode = partnerCode,
                    Memo = memo
                }));
    }

    /// <summary>
    /// 청구대기 상태의 계약 청구자료를 취소 처리한다.
    ///
    /// 청구확정 또는 납부 처리된 자료는 취소하지 않는다.
    /// 이 메서드는 잘못 생성된 청구대기 자료를 무효화할 때 사용한다.
    /// </summary>
    public async Task<int> CancelPendingChargesAsync(
    int billMonth,
    int? partnerCode,
    string? memo)
    {
        const string sql = @"
UPDATE contract_billing
SET
    bill_status = 1,
    payment_status = 0,
    bill_memo = COALESCE(@Memo, bill_memo),
    bill_udate = NOW()
WHERE bill_month = @BillMonth
  AND (@PartnerCode IS NULL OR partner_code = @PartnerCode)
  AND bill_status IN (1, 2)
  AND payment_status = 0;
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(
                sql,
                new
                {
                    BillMonth = billMonth,
                    PartnerCode = partnerCode,
                    Memo = memo
                }));
    }

    public async Task<int> CountCancelableChargesAsync(
    int billMonth,
    int? partnerCode)
    {
        const string sql = @"
SELECT COUNT(1)
FROM contract_billing
WHERE bill_month = @BillMonth
  AND (@PartnerCode IS NULL OR partner_code = @PartnerCode)
  AND bill_status IN (1, 2)
  AND payment_status = 0;
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    BillMonth = billMonth,
                    PartnerCode = partnerCode
                }));
    }


    /// <summary>
    /// 취소 가능한 계약건 조회
    /// </summary>
    /// <param name="billMonth"></param>
    /// <param name="partnerCode"></param>
    /// <returns></returns>
    public async Task<int> CountResettableConfirmedChargesAsync(
    int billMonth,
    int? partnerCode)
    {
        const string sql = @"
SELECT COUNT(1)
FROM contract_billing
WHERE bill_month = @BillMonth
  AND (@PartnerCode IS NULL OR partner_code = @PartnerCode)
  AND bill_status = 2
  AND payment_status = 0;
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    BillMonth = billMonth,
                    PartnerCode = partnerCode
                }));
    }
}
