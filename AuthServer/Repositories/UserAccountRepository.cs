using Dapper;
using poscam.AuthServer.Models.Dtos.Account;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;

namespace poscam.AuthServer.Repositories;

/// <summary>
/// users 테이블 접근 Repository.
/// 
/// 관리자/담당자 계정 조회, 가입, 승인, 차단 처리를 담당한다.
/// 실제 로그인 가능 여부, 권한 판단은 Service에서 처리한다.
/// </summary>
public class UserAccountRepository : RepositoryBase
{
    public UserAccountRepository(IDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// 사용자 코드로 계정을 조회한다.
    /// </summary>
    public async Task<UserAccount?> GetByCodeAsync(int userCode)
    {
        const string sql = @"
        SELECT
            user_code          AS UserCode,
            partner_code       AS PartnerCode,
            user_id            AS UserId,
            user_password_hash AS UserPasswordHash,
            user_name          AS UserName,
            user_cell          AS UserCell,
            user_email         AS UserEmail,
            user_role          AS UserRole,
            user_status        AS UserStatus,
            approved_by        AS ApprovedBy,
            approved_at        AS ApprovedAt,
            user_rdate         AS UserRDate,
            user_udate         AS UserUDate
        FROM users
        WHERE user_code = @UserCode;
        ";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<UserAccount>(
                sql,
                new { UserCode = userCode }));
    }

    /// <summary>
    /// 로그인 ID로 계정을 조회한다.
    /// 로그인 처리 시 사용한다.
    /// </summary>
    public async Task<UserAccount?> GetByUserIdAsync(string userId)
    {
        const string sql = @"
        SELECT
            user_code          AS UserCode,
            partner_code       AS PartnerCode,
            user_id            AS UserId,
            user_password_hash AS UserPasswordHash,
            user_name          AS UserName,
            user_cell          AS UserCell,
            user_email         AS UserEmail,
            user_role          AS UserRole,
            user_status        AS UserStatus,
            approved_by        AS ApprovedBy,
            approved_at        AS ApprovedAt,
            user_rdate         AS UserRDate,
            user_udate         AS UserUDate
        FROM users
        WHERE user_id = @UserId;
        ";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<UserAccount>(
                sql,
                new { UserId = userId }));
    }

    /// <summary>
    /// 로그인 ID 중복 여부를 확인한다.
    /// 회원가입 시 사용한다.
    /// </summary>
    public async Task<bool> ExistsUserIdAsync(string userId)
    {
        const string sql = @"
        SELECT COUNT(1)
        FROM users
        WHERE user_id = @UserId;
        ";

        var count = await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(
                sql,
                new { UserId = userId }));

        return count > 0;
    }

    /// <summary>
    /// 신규 담당자 계정을 등록한다.
    /// 담당자는 기본적으로 승인대기 상태로 등록된다.
    /// </summary>
    public async Task<int> InsertAsync(UserAccount user)
    {
        const string sql = @"
        INSERT INTO users
        (
            partner_code,
            user_id,
            user_password_hash,
            user_name,
            user_cell,
            user_email,
            user_role,
            user_status,
            user_rdate
        )
        VALUES
        (
            @PartnerCode,
            @UserId,
            @UserPasswordHash,
            @UserName,
            @UserCell,
            @UserEmail,
            @UserRole,
            @UserStatus,
            NOW()
        );

        SELECT LAST_INSERT_ID();
        ";

        return await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(sql, user));
    }

    /// <summary>
    /// 승인 대기 사용자 목록을 조회한다.
    /// 관리자 승인 화면에서 사용한다.
    /// </summary>
    public async Task<List<UserPendingListItemDto>> GetPendingUsersAsync()
    {
        const string sql = @"
        SELECT
            u.user_code    AS UserCode,
            u.partner_code AS PartnerCode,
            p.partner_name AS PartnerName,
            u.user_id      AS UserId,
            u.user_name    AS UserName,
            u.user_cell    AS UserCell,
            u.user_email   AS UserEmail,
            u.user_rdate   AS RegisteredAt
        FROM users u
        LEFT JOIN partners p
            ON u.partner_code = p.partner_code
        WHERE u.user_status = @PendingStatus
        ORDER BY u.user_rdate DESC, u.user_code DESC;
        ";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<UserPendingListItemDto>(
                sql,
                new { PendingStatus = (int)UserStatus.Pending }));

        return result.ToList();
    }

    /// <summary>
    /// 담당자 계정을 승인한다.
    /// </summary>
    public async Task<int> ApproveAsync(int userCode, int approvedBy)
    {
        const string sql = @"
        UPDATE users
        SET
            user_status = @ActiveStatus,
            approved_by = @ApprovedBy,
            approved_at = NOW(),
            user_udate = NOW()
        WHERE user_code = @UserCode;
        ";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(
                sql,
                new
                {
                    UserCode = userCode,
                    ApprovedBy = approvedBy,
                    ActiveStatus = (int)UserStatus.Active
                }));
    }

    /// <summary>
    /// 사용자 계정을 차단한다.
    /// </summary>
    public async Task<int> BlockAsync(int userCode)
    {
        const string sql = @"
        UPDATE users
        SET
            user_status = @BlockedStatus,
            user_udate = NOW()
        WHERE user_code = @UserCode;
        ";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(
                sql,
                new
                {
                    UserCode = userCode,
                    BlockedStatus = (int)UserStatus.Blocked
                }));
    }

    /// <summary>
    /// 사용자 계정을 일시중지한다.
    /// </summary>
    public async Task<int> SuspendAsync(int userCode)
    {
        const string sql = @"
        UPDATE users
        SET
            user_status = @SuspendedStatus,
            user_udate = NOW()
        WHERE user_code = @UserCode;
        ";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(
                sql,
                new
                {
                    UserCode = userCode,
                    SuspendedStatus = (int)UserStatus.Suspended
                }));
    }

    /// <summary>
    /// 개발용 관리자 계정을 생성한다.
    /// 운영용 일반 회원가입이 아니라 초기 테스트 관리자 생성용이다.
    /// </summary>
    public async Task<int> InsertAdminAsync(UserAccount user)
    {
        const string sql = @"
        INSERT INTO users
        (
            partner_code,
            user_id,
            user_password_hash,
            user_name,
            user_cell,
            user_email,
            user_role,
            user_status,
            approved_by,
            approved_at,
            user_rdate
        )
        VALUES
        (
            @PartnerCode,
            @UserId,
            @UserPasswordHash,
            @UserName,
            @UserCell,
            @UserEmail,
            @UserRole,
            @UserStatus,
            @ApprovedBy,
            NOW(),
            NOW()
        );

        SELECT LAST_INSERT_ID();
        ";

        return await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(sql, user));
    }

    /// <summary>
    /// 기존 관리자 계정의 비밀번호 해시와 상태를 개발용으로 재설정한다.
    /// 기존 admin 계정이 평문 비밀번호로 들어간 경우 이 메서드로 갱신한다.
    /// </summary>
    public async Task<int> UpdateAdminPasswordAndActivateAsync(
        int userCode,
        string passwordHash)
    {
        const string sql = @"
        UPDATE users
        SET
            user_password_hash = @PasswordHash,
            user_role = @AdminRole,
            user_status = @ActiveStatus,
            approved_at = IFNULL(approved_at, NOW()),
            user_udate = NOW()
        WHERE user_code = @UserCode;
        ";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(
                sql,
                new
                {
                    UserCode = userCode,
                    PasswordHash = passwordHash,
                    AdminRole = (int)UserRole.Admin,
                    ActiveStatus = (int)UserStatus.Active
                }));
    }

    /// <summary>
    /// 매장 담당자 배정에 사용할 활성 담당자 목록을 조회한다.
    /// 관리자 계정은 제외하고, 승인 완료된 담당자만 조회한다.
    /// </summary>
    public async Task<List<UserListItemDto>> GetActivePartnerUsersAsync()
    {
        const string sql = @"
        SELECT
            u.user_code    AS UserCode,
            u.partner_code AS PartnerCode,
            p.partner_name AS PartnerName,
            u.user_id      AS UserId,
            u.user_name    AS UserName,
            u.user_cell    AS UserCell,
            u.user_email   AS UserEmail,
            u.user_role    AS UserRole,
            u.user_status  AS UserStatus
        FROM users u
        LEFT JOIN partners p
            ON u.partner_code = p.partner_code
        WHERE u.user_role = @PartnerUserRole
          AND u.user_status = @ActiveStatus
        ORDER BY p.partner_name ASC, u.user_name ASC, u.user_code ASC;
        ";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<UserListItemDto>(
                sql,
                new
                {
                    PartnerUserRole = (int)UserRole.PartnerUser,
                    ActiveStatus = (int)UserStatus.Active
                }));

        return result.ToList();
    }

    /// <summary>
    /// 특정 파트너사에 소속된 활성 담당자 목록을 조회한다.
    /// 
    /// 매장 담당자 배정 시 사용한다.
    /// 승인 완료된 정상 담당자만 조회한다.
    /// </summary>
    public async Task<List<UserListItemDto>> GetActivePartnerUsersByPartnerAsync(
        int partnerCode)
    {
        const string sql = @"
SELECT
    u.user_code AS UserCode,
    u.partner_code AS PartnerCode,
    p.partner_name AS PartnerName,
    u.user_id AS UserId,
    u.user_name AS UserName,
    u.user_cell AS UserCell,
    u.user_email AS UserEmail,
    u.user_role AS UserRole,
    u.user_status AS UserStatus,
    u.approved_by AS ApprovedBy,
    u.approved_at AS ApprovedAt,
    u.user_rdate AS UserRdate,
    u.user_udate AS UserUdate,
    u.user_request_type AS UserRequestType,
    u.user_request_status AS UserRequestStatus,
    u.user_request_reason AS UserRequestReason,
    u.user_requested_by AS UserRequestedBy,
    u.user_requested_at AS UserRequestedAt,
    u.user_request_result_memo AS UserRequestResultMemo
FROM users u
LEFT JOIN partners p
    ON u.partner_code = p.partner_code
WHERE u.partner_code = @PartnerCode
  AND u.user_role = @PartnerUserRole
  AND u.user_status = @ActiveStatus
ORDER BY u.user_name ASC, u.user_code ASC;
";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<UserListItemDto>(
                sql,
                new
                {
                    PartnerCode = partnerCode,
                    PartnerUserRole = (int)UserRole.PartnerUser,
                    ActiveStatus = (int)UserStatus.Active
                }));

        return result.ToList();
    }

    /// <summary>
    /// users 테이블을 UserAccount Entity로 매핑하기 위한 공통 SELECT 컬럼.
    /// 
    /// Dapper에서 underscore 매핑을 사용하고 있더라도,
    /// 명확한 매핑을 위해 AS 별칭을 사용한다.
    /// </summary>
    private const string UserAccountSelectColumns = @"
    u.user_code AS UserCode,
    u.partner_code AS PartnerCode,
    u.user_id AS UserId,
    u.user_password_hash AS UserPasswordHash,
    u.user_name AS UserName,
    u.user_cell AS UserCell,
    u.user_email AS UserEmail,
    u.user_role AS UserRole,
    u.user_status AS UserStatus,
    u.approved_by AS ApprovedBy,
    u.approved_at AS ApprovedAt,
    u.user_rdate AS UserRdate,
    u.user_udate AS UserUdate,
    u.user_request_type AS UserRequestType,
    u.user_request_status AS UserRequestStatus,
    u.user_request_reason AS UserRequestReason,
    u.user_requested_by AS UserRequestedBy,
    u.user_requested_at AS UserRequestedAt,
    u.user_request_result_memo AS UserRequestResultMemo
";

    /// <summary>
    /// 파트너사 기준 담당자/직원 목록을 조회한다.
    /// 
    /// 관리자 화면:
    /// - 선택한 파트너사의 직원 목록 조회
    /// 
    /// 담당자 화면:
    /// - 본인 파트너사 직원 목록 조회
    /// 
    /// user_code는 화면에 표시하지 않지만,
    /// 상세 이동을 위해 DTO에는 포함한다.
    /// </summary>
    public async Task<List<UserListItemDto>> GetUsersByPartnerAsync(
        int partnerCode,
        int? userStatus = null,
        int? requestStatus = null)
    {
        const string sql = @"
SELECT
    u.user_code AS UserCode,
    u.partner_code AS PartnerCode,
    p.partner_name AS PartnerName,

    u.user_id AS UserId,
    u.user_name AS UserName,
    u.user_cell AS UserCell,
    u.user_email AS UserEmail,

    u.user_role AS UserRole,
    u.user_status AS UserStatus,

    u.approved_by AS ApprovedBy,
    u.approved_at AS ApprovedAt,

    u.user_rdate AS UserRdate,
    u.user_udate AS UserUdate,

    u.user_request_type AS UserRequestType,
    u.user_request_status AS UserRequestStatus,
    u.user_request_reason AS UserRequestReason,
    u.user_requested_by AS UserRequestedBy,
    u.user_requested_at AS UserRequestedAt,
    u.user_request_result_memo AS UserRequestResultMemo
FROM users u
LEFT JOIN partners p
    ON u.partner_code = p.partner_code
WHERE u.partner_code = @PartnerCode
  AND u.user_role = @PartnerUserRole
  AND (@UserStatus IS NULL OR u.user_status = @UserStatus)
  AND (@RequestStatus IS NULL OR u.user_request_status = @RequestStatus)
ORDER BY
    u.user_status ASC,
    u.user_name ASC,
    u.user_code ASC;
";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<UserListItemDto>(
                sql,
                new
                {
                    PartnerCode = partnerCode,
                    PartnerUserRole = (int)UserRole.PartnerUser,
                    UserStatus = userStatus,
                    RequestStatus = requestStatus
                }));

        return result.ToList();
    }

    /// <summary>
    /// 전체 파트너사 담당자 목록을 조회한다.
    /// 
    /// 관리자 전용 화면에서 사용한다.
    /// partnerCode가 null이면 전체 조회,
    /// 값이 있으면 특정 파트너사만 조회한다.
    /// </summary>
    public async Task<List<UserListItemDto>> GetManageUserListAsync(
        int? partnerCode = null,
        int? userStatus = null,
        int? requestStatus = null)
    {
        const string sql = @"
        SELECT
            u.user_code AS UserCode,
            u.partner_code AS PartnerCode,
            p.partner_name AS PartnerName,

            u.user_id AS UserId,
            u.user_name AS UserName,
            u.user_cell AS UserCell,
            u.user_email AS UserEmail,

            u.user_role AS UserRole,
            u.user_status AS UserStatus,

            u.approved_by AS ApprovedBy,
            u.approved_at AS ApprovedAt,

            u.user_rdate AS UserRdate,
            u.user_udate AS UserUdate,

            u.user_request_type AS UserRequestType,
            u.user_request_status AS UserRequestStatus,
            u.user_request_reason AS UserRequestReason,
            u.user_requested_by AS UserRequestedBy,
            u.user_requested_at AS UserRequestedAt,
            u.user_request_result_memo AS UserRequestResultMemo
        FROM users u
        LEFT JOIN partners p
            ON u.partner_code = p.partner_code
        WHERE u.user_role = @PartnerUserRole
          AND (@PartnerCode IS NULL OR u.partner_code = @PartnerCode)
          AND (@UserStatus IS NULL OR u.user_status = @UserStatus)
          AND (@RequestStatus IS NULL OR u.user_request_status = @RequestStatus)
        ORDER BY
            p.partner_name ASC,
            u.user_status ASC,
            u.user_name ASC,
            u.user_code ASC;
        ";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<UserListItemDto>(
                sql,
                new
                {
                    PartnerUserRole = (int)UserRole.PartnerUser,
                    PartnerCode = partnerCode,
                    UserStatus = userStatus,
                    RequestStatus = requestStatus
                }));

        return result.ToList();
    }

    /// <summary>
    /// user_code 기준 사용자 상세를 조회한다.
    /// 
    /// 관리자:
    /// - 전체 사용자 상세 조회 가능
    /// 
    /// 담당자:
    /// - Service에서 본인 파트너사 소속인지 검증 후 사용한다.
    /// </summary>
    public async Task<UserAccount?> GetManageUserDetailAsync(int userCode)
    {
        var sql = $@"
SELECT
{UserAccountSelectColumns}
FROM users u
WHERE u.user_code = @UserCode;
";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<UserAccount>(
                sql,
                new
                {
                    UserCode = userCode
                }));
    }

    /// <summary>
    /// 파트너사 담당자 계정을 신규 등록한다.
    /// 
    /// 신규 등록된 담당자는 즉시 사용 가능 상태가 아니라
    /// 승인대기 상태로 저장된다.
    /// 
    /// 기본 저장값:
    /// - user_status = Pending
    /// - user_request_type = JoinApproval
    /// - user_request_status = Pending
    /// - approved_by = NULL
    /// - approved_at = NULL
    /// </summary>
    public async Task<int> InsertPartnerUserAsync(UserAccount user)
    {
        const string sql = @"
INSERT INTO users
(
    partner_code,
    user_id,
    user_password_hash,
    user_name,
    user_cell,
    user_email,
    user_role,
    user_status,

    approved_by,
    approved_at,

    user_rdate,
    user_udate,

    user_request_type,
    user_request_status,
    user_request_reason,
    user_requested_by,
    user_requested_at,
    user_request_result_memo
)
VALUES
(
    @PartnerCode,
    @UserId,
    @UserPasswordHash,
    @UserName,
    @UserCell,
    @UserEmail,
    @UserRole,
    @UserStatus,

    NULL,
    NULL,

    NOW(),
    NULL,

    @UserRequestType,
    @UserRequestStatus,
    @UserRequestReason,
    @UserRequestedBy,
    @UserRequestedAt,
    NULL
);

SELECT LAST_INSERT_ID();
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    user.PartnerCode,
                    user.UserId,
                    user.UserPasswordHash,
                    user.UserName,
                    user.UserCell,
                    user.UserEmail,

                    UserRole = (int)UserRole.PartnerUser,
                    UserStatus = (int)UserStatus.Pending,

                    UserRequestType = (int)UserRequestType.JoinApproval,
                    UserRequestStatus = (int)UserRequestStatus.Pending,
                    user.UserRequestReason,
                    user.UserRequestedBy,
                    UserRequestedAt = DateTime.Now
                }));
    }

    /// <summary>
    /// 담당자 기본정보를 수정한다.
    /// 
    /// 이 메서드는 실제 정보 변경용이다.
    /// 담당자가 직접 수정 요청을 하는 경우에는 바로 이 메서드를 호출하지 않고,
    /// 요청 상태 저장 + userlog 기록 후 관리자가 처리할 때 호출한다.
    /// </summary>
    public async Task<int> UpdateUserInfoAsync(UserAccount user)
    {
        const string sql = @"
UPDATE users
SET
    partner_code = @PartnerCode,
    user_name = @UserName,
    user_cell = @UserCell,
    user_email = @UserEmail,
    user_udate = NOW()
WHERE user_code = @UserCode;
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(
                sql,
                new
                {
                    user.UserCode,
                    user.PartnerCode,
                    user.UserName,
                    user.UserCell,
                    user.UserEmail
                }));
    }

    /// <summary>
    /// users 테이블에 최신 요청 상태를 저장한다.
    /// 
    /// userlog에는 전체 요청 이력을 누적 기록하고,
    /// users에는 가장 최근 요청 상태만 보관한다.
    /// </summary>
    public async Task<int> UpdateLatestRequestAsync(
        int userCode,
        int requestType,
        string? requestReason,
        int requestedBy)
    {
        const string sql = @"
UPDATE users
SET
    user_request_type = @RequestType,
    user_request_status = @RequestStatus,
    user_request_reason = @RequestReason,
    user_requested_by = @RequestedBy,
    user_requested_at = NOW(),
    user_request_result_memo = NULL,
    user_udate = NOW()
WHERE user_code = @UserCode;
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(
                sql,
                new
                {
                    UserCode = userCode,
                    RequestType = requestType,
                    RequestStatus = (int)UserRequestStatus.Pending,
                    RequestReason = requestReason,
                    RequestedBy = requestedBy
                }));
    }

    /// <summary>
    /// 사용자의 최근 요청을 반려 상태로 변경한다.
    /// 
    /// 실제 user_status는 변경하지 않는다.
    /// 요청 상태만 Rejected로 바꾼다.
    /// </summary>
    public async Task<int> RejectLatestRequestAsync(
        int userCode,
        string? resultMemo)
    {
        const string sql = @"
UPDATE users
SET
    user_request_status = @RequestStatus,
    user_request_result_memo = @ResultMemo,
    user_udate = NOW()
WHERE user_code = @UserCode;
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(
                sql,
                new
                {
                    UserCode = userCode,
                    RequestStatus = (int)UserRequestStatus.Rejected,
                    ResultMemo = resultMemo
                }));
    }

    /// <summary>
    /// 담당자 가입 승인 처리.
    /// 
    /// 승인 시:
    /// - user_status = Active
    /// - approved_by = 관리자 user_code
    /// - approved_at = NOW()
    /// - user_request_status = Completed
    /// - user_request_result_memo 저장
    /// </summary>
    public async Task<int> ApproveUserAsync(
        int userCode,
        int approvedBy,
        string? resultMemo)
    {
        const string sql = @"
UPDATE users
SET
    user_status = @ActiveStatus,
    approved_by = @ApprovedBy,
    approved_at = NOW(),
    user_request_status = @RequestStatus,
    user_request_result_memo = @ResultMemo,
    user_udate = NOW()
WHERE user_code = @UserCode
  AND user_status = @PendingStatus;
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(
                sql,
                new
                {
                    UserCode = userCode,
                    ApprovedBy = approvedBy,
                    ActiveStatus = (int)UserStatus.Active,
                    PendingStatus = (int)UserStatus.Pending,
                    RequestStatus = (int)UserRequestStatus.Completed,
                    ResultMemo = resultMemo
                }));
    }

    /// <summary>
    /// 사용자 상태를 변경한다.
    /// 
    /// 예:
    /// - 일시중지
    /// - 정상복구
    /// - 무효
    /// - 차단
    /// 
    /// 상태 변경은 관리자만 수행하며,
    /// 담당자는 요청만 등록한다.
    /// 권한 판단은 Service에서 처리한다.
    /// </summary>
    public async Task<int> ChangeUserStatusAsync(
        int userCode,
        int newStatus,
        int? requestType,
        string? resultMemo)
    {
        const string sql = @"
UPDATE users
SET
    user_status = @NewStatus,
    user_request_type = @RequestType,
    user_request_status = @RequestStatus,
    user_request_result_memo = @ResultMemo,
    user_udate = NOW()
WHERE user_code = @UserCode;
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(
                sql,
                new
                {
                    UserCode = userCode,
                    NewStatus = newStatus,
                    RequestType = requestType,
                    RequestStatus = (int)UserRequestStatus.Completed,
                    ResultMemo = resultMemo
                }));
    }

    /// <summary>
    /// 사용자 비밀번호 해시를 변경한다.
    /// 
    /// 실제 비밀번호 검증은 Service에서 처리하고,
    /// Repository는 해시값만 저장한다.
    /// </summary>
    public async Task<int> UpdatePasswordAsync(
        int userCode,
        string passwordHash)
    {
        const string sql = @"
UPDATE users
SET
    user_password_hash = @PasswordHash,
    user_udate = NOW()
WHERE user_code = @UserCode;
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(
                sql,
                new
                {
                    UserCode = userCode,
                    PasswordHash = passwordHash
                }));
    }

    /// <summary>
    /// 담당자의 현재 비밀번호 해시를 조회합니다.
    /// 
    /// 실제 저장 컬럼은 users.user_password_hash 입니다.
    /// 본인 비밀번호 변경 시 현재 비밀번호 검증에 사용합니다.
    /// </summary>
    public async Task<string?> GetPasswordHashAsync(int userCode)
    {
        const string sql = @"
SELECT user_password_hash
FROM users
WHERE user_code = @UserCode
LIMIT 1;
";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<string?>(
                sql,
                new
                {
                    UserCode = userCode
                }));
    }

    /// <summary>
    /// 관리자 계정 목록을 조회한다.
    /// 
    /// System 계정은 제외하고,
    /// UserRole.Admin 계정만 조회한다.
    /// </summary>
    public async Task<List<UserListItemDto>> GetAdminAccountListAsync(
        int? userStatus = null)
    {
        const string sql = @"
SELECT
    u.user_code AS UserCode,
    u.partner_code AS PartnerCode,
    NULL AS PartnerName,

    u.user_id AS UserId,
    u.user_name AS UserName,
    u.user_cell AS UserCell,
    u.user_email AS UserEmail,

    u.user_role AS UserRole,
    u.user_status AS UserStatus,

    u.approved_by AS ApprovedBy,
    u.approved_at AS ApprovedAt,

    u.user_rdate AS UserRdate,
    u.user_udate AS UserUdate,

    u.user_request_type AS UserRequestType,
    u.user_request_status AS UserRequestStatus,
    u.user_request_reason AS UserRequestReason,
    u.user_requested_by AS UserRequestedBy,
    u.user_requested_at AS UserRequestedAt,
    u.user_request_result_memo AS UserRequestResultMemo
FROM users u
WHERE u.user_role = @AdminRole
  AND (@UserStatus IS NULL OR u.user_status = @UserStatus)
ORDER BY
    u.user_status ASC,
    u.user_name ASC,
    u.user_code ASC;
";

        var result = await WithConnectionAsync(conn =>
            conn.QueryAsync<UserListItemDto>(
                sql,
                new
                {
                    AdminRole = (int)UserRole.Admin,
                    UserStatus = userStatus
                }));

        return result.ToList();
    }

    /// <summary>
    /// 관리자 계정 상세 정보를 조회한다.
    /// 
    /// System 계정은 이 메서드의 관리 대상에서 제외한다.
    /// </summary>
    public async Task<UserAccount?> GetAdminAccountDetailAsync(int userCode)
    {
        var sql = $@"
SELECT
{UserAccountSelectColumns}
FROM users u
WHERE u.user_code = @UserCode
  AND u.user_role = @AdminRole;
";

        return await WithConnectionAsync(conn =>
            conn.QueryFirstOrDefaultAsync<UserAccount>(
                sql,
                new
                {
                    UserCode = userCode,
                    AdminRole = (int)UserRole.Admin
                }));
    }

    /// <summary>
    /// 운영용 관리자 계정을 신규 등록한다.
    /// 
    /// 관리자 계정은 partner_code를 사용하지 않으며,
    /// UserRole.Admin으로 저장한다.
    /// </summary>
    public async Task<int> InsertAdminAccountAsync(UserAccount user)
    {
        const string sql = @"
INSERT INTO users
(
    partner_code,
    user_id,
    user_password_hash,
    user_name,
    user_cell,
    user_email,
    user_role,
    user_status,

    approved_by,
    approved_at,

    user_rdate,
    user_udate,

    user_request_type,
    user_request_status,
    user_request_reason,
    user_requested_by,
    user_requested_at,
    user_request_result_memo
)
VALUES
(
    NULL,
    @UserId,
    @UserPasswordHash,
    @UserName,
    @UserCell,
    @UserEmail,
    @AdminRole,
    @UserStatus,

    @ApprovedBy,
    NOW(),

    NOW(),
    NULL,

    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL
);

SELECT LAST_INSERT_ID();
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    user.UserId,
                    user.UserPasswordHash,
                    user.UserName,
                    user.UserCell,
                    user.UserEmail,
                    AdminRole = (int)UserRole.Admin,
                    user.UserStatus,
                    user.ApprovedBy
                }));
    }

    /// <summary>
    /// 관리자 계정의 기본정보를 수정한다.
    /// 
    /// 비밀번호와 권한은 별도 메서드/API에서 처리한다.
    /// </summary>
    public async Task<int> UpdateAdminAccountInfoAsync(UserAccount user)
    {
        const string sql = @"
UPDATE users
SET
    user_name = @UserName,
    user_cell = @UserCell,
    user_email = @UserEmail,
    user_status = @UserStatus,
    user_udate = NOW()
WHERE user_code = @UserCode
  AND user_role = @AdminRole;
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(
                sql,
                new
                {
                    user.UserCode,
                    user.UserName,
                    user.UserCell,
                    user.UserEmail,
                    user.UserStatus,
                    AdminRole = (int)UserRole.Admin
                }));
    }

    /// <summary>
    /// 관리자 계정 상태를 변경한다.
    /// 
    /// System 계정은 변경 대상에서 제외하고,
    /// UserRole.Admin 계정만 상태 변경한다.
    /// </summary>
    public async Task<int> ChangeAdminAccountStatusAsync(
        int userCode,
        int userStatus)
    {
        const string sql = @"
UPDATE users
SET
    user_status = @UserStatus,
    user_udate = NOW()
WHERE user_code = @UserCode
  AND user_role = @AdminRole;
";

        return await WithConnectionAsync(conn =>
            conn.ExecuteAsync(
                sql,
                new
                {
                    UserCode = userCode,
                    UserStatus = userStatus,
                    AdminRole = (int)UserRole.Admin
                }));
    }
}