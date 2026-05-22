using Dapper;
using poscam.AuthServer.Models.Dtos.Settlement;
using poscam.AuthServer.Models.Entities;

namespace poscam.AuthServer.Repositories;

/// <summary>
/// 파트너사별 월 납부 처리 Repository.
///
/// DB 테이블: billing_payment
///
/// 역할:
/// - 파트너사 + 청구월 기준 납부 처리 조회
/// - 납부 처리 신규 등록
/// - 납부 처리 수정
/// - 파트너사 월 정산 기준 납부 상태 관리
///
/// 운영 기준:
/// - contract_billing은 계약별 청구자료를 저장한다.
/// - billing_payment는 파트너사 + 청구월 단위 납부 처리 결과를 저장한다.
/// - 납부 저장 후 contract_billing.payment_status 동기화는 Service에서 ContractBillingRepository를 함께 호출해 처리한다.
/// </summary>
public class BillingPaymentRepository : RepositoryBase
{
    public BillingPaymentRepository(IDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <summary>
    /// 파트너사별 월 납부 처리 목록을 조회한다.
    ///
    /// 납부 처리 화면에서 사용한다.
    /// partnerCode가 null이면 전체 파트너사를 조회한다.
    /// payStatus가 null이면 전체 상태를 조회한다.
    /// </summary>
    /// <param name="billMonth">청구월. 예: 202605</param>
    /// <param name="partnerCode">파트너사 코드. null이면 전체.</param>
    /// <param name="payStatus">납부 상태. null이면 전체.</param>
    /// <returns>납부 처리 목록</returns>
    public async Task<List<BillingPaymentDto>> GetListAsync(
        int billMonth,
        int? partnerCode = null,
        int? payStatus = null)
    {
        const string sql = @"
SELECT
    bp.pay_code AS PayCode,
    bp.bill_month AS BillMonth,
    bp.partner_code AS PartnerCode,
    p.partner_name AS PartnerName,

    bp.pay_bill_amount AS PayBillAmount,
    bp.pay_amount AS PayAmount,
    bp.pay_remain_amount AS PayRemainAmount,
    bp.pay_status AS PayStatus,

    bp.pay_date AS PayDate,
    bp.pay_method AS PayMethod,
    bp.pay_memo AS PayMemo,
    bp.pay_created_by AS PayCreatedBy,

    bp.pay_rdate AS PayRdate,
    bp.pay_udate AS PayUdate
FROM billing_payment bp
LEFT JOIN partners p
    ON bp.partner_code = p.partner_code
WHERE bp.bill_month = @BillMonth
  AND (@PartnerCode IS NULL OR bp.partner_code = @PartnerCode)
  AND (@PayStatus IS NULL OR bp.pay_status = @PayStatus)
ORDER BY
    p.partner_name ASC,
    bp.partner_code ASC;
";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<BillingPaymentDto>(
                sql,
                new
                {
                    BillMonth = billMonth,
                    PartnerCode = partnerCode,
                    PayStatus = payStatus
                }));

        return result.ToList();
    }

    /// <summary>
    /// pay_code 기준으로 납부 처리 정보를 조회한다.
    ///
    /// 납부 상세 조회 또는 수정 전 검증에 사용한다.
    /// </summary>
    /// <param name="payCode">납부 처리 코드</param>
    /// <returns>납부 처리 Entity</returns>
    public async Task<BillingPayment?> GetByCodeAsync(int payCode)
    {
        const string sql = @"
SELECT
    pay_code AS PayCode,
    bill_month AS BillMonth,
    partner_code AS PartnerCode,

    pay_bill_amount AS PayBillAmount,
    pay_amount AS PayAmount,
    pay_remain_amount AS PayRemainAmount,
    pay_status AS PayStatus,

    pay_date AS PayDate,
    pay_method AS PayMethod,
    pay_memo AS PayMemo,
    pay_created_by AS PayCreatedBy,

    pay_rdate AS PayRdate,
    pay_udate AS PayUdate
FROM billing_payment
WHERE pay_code = @PayCode;
";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<BillingPayment>(
                sql,
                new
                {
                    PayCode = payCode
                }));
    }

    /// <summary>
    /// 청구월 + 파트너사 기준으로 납부 처리 정보를 조회한다.
    ///
    /// partner_code + bill_month는 unique 기준이다.
    /// </summary>
    /// <param name="billMonth">청구월</param>
    /// <param name="partnerCode">파트너사 코드</param>
    /// <returns>납부 처리 Entity</returns>
    public async Task<BillingPayment?> GetByPartnerMonthAsync(
        int billMonth,
        int partnerCode)
    {
        const string sql = @"
SELECT
    pay_code AS PayCode,
    bill_month AS BillMonth,
    partner_code AS PartnerCode,

    pay_bill_amount AS PayBillAmount,
    pay_amount AS PayAmount,
    pay_remain_amount AS PayRemainAmount,
    pay_status AS PayStatus,

    pay_date AS PayDate,
    pay_method AS PayMethod,
    pay_memo AS PayMemo,
    pay_created_by AS PayCreatedBy,

    pay_rdate AS PayRdate,
    pay_udate AS PayUdate
FROM billing_payment
WHERE bill_month = @BillMonth
  AND partner_code = @PartnerCode;
";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<BillingPayment>(
                sql,
                new
                {
                    BillMonth = billMonth,
                    PartnerCode = partnerCode
                }));
    }

    /// <summary>
    /// 청구월 + 파트너사 기준 납부 처리 정보 존재 여부를 확인한다.
    /// </summary>
    /// <param name="billMonth">청구월</param>
    /// <param name="partnerCode">파트너사 코드</param>
    /// <returns>존재 여부</returns>
    public async Task<bool> ExistsAsync(
        int billMonth,
        int partnerCode)
    {
        const string sql = @"
SELECT COUNT(1)
FROM billing_payment
WHERE bill_month = @BillMonth
  AND partner_code = @PartnerCode;
";

        var count = await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    BillMonth = billMonth,
                    PartnerCode = partnerCode
                }));

        return count > 0;
    }

    /// <summary>
    /// 납부 처리 정보를 신규 등록한다.
    ///
    /// 월별 청구자료 생성 후 파트너사별 납부 대상 초기 row를 만들 때 사용한다.
    /// </summary>
    /// <param name="payment">등록할 납부 처리 Entity</param>
    /// <returns>생성된 pay_code</returns>
    public async Task<int> InsertAsync(BillingPayment payment)
    {
        const string sql = @"
INSERT INTO billing_payment
(
    bill_month,
    partner_code,

    pay_bill_amount,
    pay_amount,
    pay_remain_amount,
    pay_status,

    pay_date,
    pay_method,
    pay_memo,
    pay_created_by,

    pay_rdate,
    pay_udate
)
VALUES
(
    @BillMonth,
    @PartnerCode,

    @PayBillAmount,
    @PayAmount,
    @PayRemainAmount,
    @PayStatus,

    @PayDate,
    @PayMethod,
    @PayMemo,
    @PayCreatedBy,

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
                    payment.BillMonth,
                    payment.PartnerCode,
                    payment.PayBillAmount,
                    payment.PayAmount,
                    payment.PayRemainAmount,
                    payment.PayStatus,
                    payment.PayDate,
                    payment.PayMethod,
                    payment.PayMemo,
                    payment.PayCreatedBy
                }));
    }

    /// <summary>
    /// 납부 처리 정보를 수정한다.
    ///
    /// 관리자 납부 처리 화면에서 입금액, 납부일, 납부방식, 처리메모 등을 수정할 때 사용한다.
    /// </summary>
    /// <param name="payment">수정할 납부 처리 Entity</param>
    /// <returns>영향받은 행 수</returns>
    public async Task<int> UpdateAsync(BillingPayment payment)
    {
        const string sql = @"
UPDATE billing_payment
SET
    pay_bill_amount = @PayBillAmount,
    pay_amount = @PayAmount,
    pay_remain_amount = @PayRemainAmount,
    pay_status = @PayStatus,

    pay_date = @PayDate,
    pay_method = @PayMethod,
    pay_memo = @PayMemo,
    pay_created_by = @PayCreatedBy,

    pay_udate = NOW()
WHERE pay_code = @PayCode;
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(
                sql,
                new
                {
                    payment.PayCode,
                    payment.PayBillAmount,
                    payment.PayAmount,
                    payment.PayRemainAmount,
                    payment.PayStatus,
                    payment.PayDate,
                    payment.PayMethod,
                    payment.PayMemo,
                    payment.PayCreatedBy
                }));
    }

    /// <summary>
    /// 청구월 + 파트너사 기준 납부 처리 정보를 등록 또는 수정한다.
    ///
    /// billing_payment에는 UNIQUE KEY uq_billing_payment_month_partner가 있다.
    /// 이미 row가 있으면 update하고, 없으면 insert한다.
    ///
    /// SELECT LAST_INSERT_ID()를 통해 insert/update 모두 pay_code를 반환한다.
    /// </summary>
    /// <param name="payment">저장할 납부 처리 Entity</param>
    /// <returns>저장된 pay_code</returns>
    public async Task<int> UpsertByPartnerMonthAsync(BillingPayment payment)
    {
        const string sql = @"
INSERT INTO billing_payment
(
    bill_month,
    partner_code,

    pay_bill_amount,
    pay_amount,
    pay_remain_amount,
    pay_status,

    pay_date,
    pay_method,
    pay_memo,
    pay_created_by,

    pay_rdate,
    pay_udate
)
VALUES
(
    @BillMonth,
    @PartnerCode,

    @PayBillAmount,
    @PayAmount,
    @PayRemainAmount,
    @PayStatus,

    @PayDate,
    @PayMethod,
    @PayMemo,
    @PayCreatedBy,

    NOW(),
    NULL
)
ON DUPLICATE KEY UPDATE
    pay_code = LAST_INSERT_ID(pay_code),
    pay_bill_amount = VALUES(pay_bill_amount),
    pay_amount = VALUES(pay_amount),
    pay_remain_amount = VALUES(pay_remain_amount),
    pay_status = VALUES(pay_status),
    pay_date = VALUES(pay_date),
    pay_method = VALUES(pay_method),
    pay_memo = VALUES(pay_memo),
    pay_created_by = VALUES(pay_created_by),
    pay_udate = NOW();

SELECT LAST_INSERT_ID();
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    payment.BillMonth,
                    payment.PartnerCode,
                    payment.PayBillAmount,
                    payment.PayAmount,
                    payment.PayRemainAmount,
                    payment.PayStatus,
                    payment.PayDate,
                    payment.PayMethod,
                    payment.PayMemo,
                    payment.PayCreatedBy
                }));
    }

    /// <summary>
    /// 납부 상태만 변경한다.
    ///
    /// 보류, 취소 등 상태만 빠르게 바꿀 때 사용한다.
    /// </summary>
    /// <param name="payCode">납부 처리 코드</param>
    /// <param name="payStatus">변경할 납부 상태</param>
    /// <param name="memo">처리 메모</param>
    /// <param name="processedBy">처리한 관리자 user_code</param>
    /// <returns>영향받은 행 수</returns>
    public async Task<int> UpdateStatusAsync(
        int payCode,
        int payStatus,
        string? memo,
        int? processedBy)
    {
        const string sql = @"
UPDATE billing_payment
SET
    pay_status = @PayStatus,
    pay_memo = COALESCE(@Memo, pay_memo),
    pay_created_by = COALESCE(@ProcessedBy, pay_created_by),
    pay_udate = NOW()
WHERE pay_code = @PayCode;
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(
                sql,
                new
                {
                    PayCode = payCode,
                    PayStatus = payStatus,
                    Memo = memo,
                    ProcessedBy = processedBy
                }));
    }

    /// <summary>
    /// 월별 청구자료 생성 후 파트너사별 납부 대상 초기 row를 생성한다.
    ///
    /// 이미 존재하는 billing_payment row는 건드리지 않는다.
    /// 초기 생성값:
    /// - pay_bill_amount = contract_billing 합산금액
    /// - pay_amount = 0
    /// - pay_remain_amount = 합산금액
    /// - pay_status = 0 미처리
    ///
    /// 생성 대상:
    /// - contract_billing.bill_status <> 9
    /// - 해당 bill_month의 파트너사별 합산금액
    /// </summary>
    /// <param name="billMonth">청구월</param>
    /// <param name="partnerCode">특정 파트너사만 생성할 경우 사용. null이면 전체.</param>
    /// <returns>생성된 행 수</returns>
    public async Task<int> CreateInitialPaymentsFromBillingAsync(
        int billMonth,
        int? partnerCode = null)
    {
        const string sql = @"
INSERT INTO billing_payment
(
    bill_month,
    partner_code,
    pay_bill_amount,
    pay_amount,
    pay_remain_amount,
    pay_status,
    pay_date,
    pay_method,
    pay_memo,
    pay_created_by,
    pay_rdate,
    pay_udate
)
SELECT
    cb.bill_month,
    cb.partner_code,
    SUM(cb.bill_total_amount) AS pay_bill_amount,
    0 AS pay_amount,
    SUM(cb.bill_total_amount) AS pay_remain_amount,
    0 AS pay_status,
    NULL AS pay_date,
    NULL AS pay_method,
    NULL AS pay_memo,
    NULL AS pay_created_by,
    NOW() AS pay_rdate,
    NULL AS pay_udate
FROM contract_billing cb
WHERE cb.bill_month = @BillMonth
  AND cb.bill_status <> 9
  AND (@PartnerCode IS NULL OR cb.partner_code = @PartnerCode)
  AND NOT EXISTS
  (
      SELECT 1
      FROM billing_payment bp
      WHERE bp.bill_month = cb.bill_month
        AND bp.partner_code = cb.partner_code
  )
GROUP BY cb.bill_month, cb.partner_code;
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
    /// 현재 contract_billing 합산금액을 기준으로 billing_payment의 청구금액을 재계산한다.
    ///
    /// 사용 시점:
    /// - 청구대기 자료를 재생성한 뒤
    /// - 아직 납부 처리 전인 payment row의 청구금액을 갱신할 때
    ///
    /// 주의:
    /// - 이미 부분납부/납부완료된 row는 이 메서드로 갱신하지 않는 것이 안전하다.
    /// - Service에서 PayStatus가 미처리 또는 미납인지 확인 후 호출하는 것을 권장한다.
    /// </summary>
    /// <param name="billMonth">청구월</param>
    /// <param name="partnerCode">파트너사 코드</param>
    /// <returns>영향받은 행 수</returns>
    public async Task<int> RefreshBillAmountAsync(
        int billMonth,
        int partnerCode)
    {
        const string sql = @"
UPDATE billing_payment bp
JOIN
(
    SELECT
        bill_month,
        partner_code,
        SUM(bill_total_amount) AS total_amount
    FROM contract_billing
    WHERE bill_status <> 9
    GROUP BY bill_month, partner_code
) x
    ON bp.bill_month = x.bill_month
   AND bp.partner_code = x.partner_code
SET
    bp.pay_bill_amount = x.total_amount,
    bp.pay_remain_amount = CASE
        WHEN bp.pay_status IN (0, 1) THEN x.total_amount
        WHEN bp.pay_status = 9 THEN 0
        ELSE GREATEST(x.total_amount - bp.pay_amount, 0)
    END,
    bp.pay_status = CASE
        WHEN bp.pay_status = 0 THEN 0
        WHEN bp.pay_status = 9 THEN 9
        WHEN bp.pay_amount <= 0 THEN 1
        WHEN bp.pay_amount < x.total_amount THEN 2
        ELSE 3
    END,
    bp.pay_udate = NOW();
WHERE bp.bill_month = @BillMonth
  AND bp.partner_code = @PartnerCode;
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
    /// 특정 청구월 + 파트너사 기준 납부 처리 row를 취소 상태로 변경한다.
    ///
    /// 실제 삭제 대신 상태값을 9로 변경하여 이력을 보존한다.
    /// </summary>
    /// <param name="billMonth">청구월</param>
    /// <param name="partnerCode">파트너사 코드</param>
    /// <param name="memo">취소 메모</param>
    /// <param name="processedBy">처리 관리자 user_code</param>
    /// <returns>영향받은 행 수</returns>
    public async Task<int> CancelByPartnerMonthAsync(
        int billMonth,
        int partnerCode,
        string? memo,
        int? processedBy)
    {
        const string sql = @"
UPDATE billing_payment
SET
    pay_status = 9,
    pay_memo = COALESCE(@Memo, pay_memo),
    pay_created_by = COALESCE(@ProcessedBy, pay_created_by),
    pay_udate = NOW()
WHERE bill_month = @BillMonth
  AND partner_code = @PartnerCode;
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(
                sql,
                new
                {
                    BillMonth = billMonth,
                    PartnerCode = partnerCode,
                    Memo = memo,
                    ProcessedBy = processedBy
                }));
    }

    public async Task<int> CancelUnprocessedByMonthAsync(
    int billMonth,
    int? partnerCode,
    string? memo,
    int? processedBy)
    {
        const string sql = @"
UPDATE billing_payment
SET
    pay_amount = 0,
    pay_remain_amount = 0,
    pay_status = 9,
    pay_memo = COALESCE(@Memo, pay_memo),
    pay_created_by = COALESCE(@ProcessedBy, pay_created_by),
    pay_udate = NOW()
WHERE bill_month = @BillMonth
  AND (@PartnerCode IS NULL OR partner_code = @PartnerCode)
  AND pay_status IN (0, 1);
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(
                sql,
                new
                {
                    BillMonth = billMonth,
                    PartnerCode = partnerCode,
                    Memo = memo,
                    ProcessedBy = processedBy
                }));
    }
}
