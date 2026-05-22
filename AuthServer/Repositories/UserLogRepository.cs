using Dapper;
using poscam.AuthServer.Models.Entities;

namespace poscam.AuthServer.Repositories;

/// <summary>
/// 사용자 계정 로그 Repository.
/// 
/// userlog 테이블에 사용자 계정 관련 이력을 기록하고 조회한다.
/// 
/// 이 Repository는 다음과 같은 이력을 관리한다.
/// - 담당자 계정 등록
/// - 가입 승인 요청
/// - 가입 승인 처리
/// - 정보 수정 요청/처리
/// - 비밀번호 변경/초기화
/// - 일시중지 요청/처리
/// - 정상복구 요청/처리
/// - 무효 요청/처리
/// - 차단 요청/처리
/// </summary>
public class UserLogRepository : RepositoryBase
{
    public UserLogRepository(IDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <summary>
    /// 사용자 로그를 신규 등록한다.
    /// 
    /// insert 후 생성된 ulog_code를 반환한다.
    /// </summary>
    /// <param name="log">등록할 사용자 로그 Entity</param>
    /// <returns>생성된 ulog_code</returns>
    public async Task<int> InsertAsync(UserLog log)
    {
        const string sql = @"
INSERT INTO userlog
(
    user_code,
    partner_code,

    ulog_type,
    ulog_request_type,
    ulog_request_status,

    ulog_before_status,
    ulog_after_status,

    ulog_reason,
    ulog_memo,

    ulog_changed_fields,

    ulog_requested_by,
    ulog_processed_by,

    ulog_requested_at,
    ulog_processed_at,

    ulog_rdate
)
VALUES
(
    @UserCode,
    @PartnerCode,

    @UlogType,
    @UlogRequestType,
    @UlogRequestStatus,

    @UlogBeforeStatus,
    @UlogAfterStatus,

    @UlogReason,
    @UlogMemo,

    @UlogChangedFields,

    @UlogRequestedBy,
    @UlogProcessedBy,

    @UlogRequestedAt,
    @UlogProcessedAt,

    NOW()
);

SELECT LAST_INSERT_ID();
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    log.UserCode,
                    log.PartnerCode,

                    log.UlogType,
                    log.UlogRequestType,
                    log.UlogRequestStatus,

                    log.UlogBeforeStatus,
                    log.UlogAfterStatus,

                    log.UlogReason,
                    log.UlogMemo,

                    log.UlogChangedFields,

                    log.UlogRequestedBy,
                    log.UlogProcessedBy,

                    log.UlogRequestedAt,
                    log.UlogProcessedAt
                }));
    }

    /// <summary>
    /// 특정 사용자 기준 로그 목록을 조회한다.
    /// 
    /// 담당자 상세 화면에서 해당 담당자의 변경 이력,
    /// 승인 이력, 요청 이력을 보여줄 때 사용한다.
    /// </summary>
    /// <param name="userCode">대상 사용자 코드</param>
    /// <param name="limit">조회 개수 제한</param>
    /// <returns>사용자 로그 목록</returns>
    public async Task<List<UserLog>> GetByUserCodeAsync(
        int userCode,
        int limit = 100)
    {
        const string sql = @"
SELECT
    ulog_code          AS UlogCode,
    user_code          AS UserCode,
    partner_code       AS PartnerCode,

    ulog_type          AS UlogType,
    ulog_request_type  AS UlogRequestType,
    ulog_request_status AS UlogRequestStatus,

    ulog_before_status AS UlogBeforeStatus,
    ulog_after_status  AS UlogAfterStatus,

    ulog_reason        AS UlogReason,
    ulog_memo          AS UlogMemo,

    ulog_changed_fields AS UlogChangedFields,

    ulog_requested_by  AS UlogRequestedBy,
    ulog_processed_by  AS UlogProcessedBy,

    ulog_requested_at  AS UlogRequestedAt,
    ulog_processed_at  AS UlogProcessedAt,

    ulog_rdate         AS UlogRdate
FROM userlog
WHERE user_code = @UserCode
ORDER BY ulog_code DESC
LIMIT @Limit;
";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<UserLog>(
                sql,
                new
                {
                    UserCode = userCode,
                    Limit = limit
                }));

        return result.ToList();
    }

    /// <summary>
    /// 특정 파트너사 기준 사용자 로그 목록을 조회한다.
    /// 
    /// 파트너사 직원 관리 화면에서 해당 파트너사 소속 직원들의
    /// 요청/처리 이력을 확인할 때 사용한다.
    /// </summary>
    /// <param name="partnerCode">파트너사 코드</param>
    /// <param name="limit">조회 개수 제한</param>
    /// <returns>파트너사 사용자 로그 목록</returns>
    public async Task<List<UserLog>> GetByPartnerCodeAsync(
        int partnerCode,
        int limit = 200)
    {
        const string sql = @"
SELECT
    ulog_code          AS UlogCode,
    user_code          AS UserCode,
    partner_code       AS PartnerCode,

    ulog_type          AS UlogType,
    ulog_request_type  AS UlogRequestType,
    ulog_request_status AS UlogRequestStatus,

    ulog_before_status AS UlogBeforeStatus,
    ulog_after_status  AS UlogAfterStatus,

    ulog_reason        AS UlogReason,
    ulog_memo          AS UlogMemo,

    ulog_changed_fields AS UlogChangedFields,

    ulog_requested_by  AS UlogRequestedBy,
    ulog_processed_by  AS UlogProcessedBy,

    ulog_requested_at  AS UlogRequestedAt,
    ulog_processed_at  AS UlogProcessedAt,

    ulog_rdate         AS UlogRdate
FROM userlog
WHERE partner_code = @PartnerCode
ORDER BY ulog_code DESC
LIMIT @Limit;
";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<UserLog>(
                sql,
                new
                {
                    PartnerCode = partnerCode,
                    Limit = limit
                }));

        return result.ToList();
    }

    /// <summary>
    /// 요청 대기 상태의 사용자 로그 목록을 조회한다.
    /// 
    /// 관리자 화면에서 처리해야 할 요청 목록을 볼 때 사용한다.
    /// partnerCode가 null이면 전체 파트너사의 요청을 조회하고,
    /// partnerCode가 있으면 해당 파트너사의 요청만 조회한다.
    /// </summary>
    /// <param name="partnerCode">파트너사 코드. null이면 전체 조회</param>
    /// <param name="limit">조회 개수 제한</param>
    /// <returns>요청 대기 로그 목록</returns>
    public async Task<List<UserLog>> GetPendingRequestsAsync(
        int? partnerCode = null,
        int limit = 200)
    {
        const string sql = @"
SELECT
    ulog_code          AS UlogCode,
    user_code          AS UserCode,
    partner_code       AS PartnerCode,

    ulog_type          AS UlogType,
    ulog_request_type  AS UlogRequestType,
    ulog_request_status AS UlogRequestStatus,

    ulog_before_status AS UlogBeforeStatus,
    ulog_after_status  AS UlogAfterStatus,

    ulog_reason        AS UlogReason,
    ulog_memo          AS UlogMemo,

    ulog_changed_fields AS UlogChangedFields,

    ulog_requested_by  AS UlogRequestedBy,
    ulog_processed_by  AS UlogProcessedBy,

    ulog_requested_at  AS UlogRequestedAt,
    ulog_processed_at  AS UlogProcessedAt,

    ulog_rdate         AS UlogRdate
FROM userlog
WHERE ulog_request_status = @PendingStatus
  AND (@PartnerCode IS NULL OR partner_code = @PartnerCode)
ORDER BY ulog_code DESC
LIMIT @Limit;
";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<UserLog>(
                sql,
                new
                {
                    PendingStatus = 1,
                    PartnerCode = partnerCode,
                    Limit = limit
                }));

        return result.ToList();
    }
}