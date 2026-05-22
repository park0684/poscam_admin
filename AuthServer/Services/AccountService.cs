using poscam.AuthServer.Models.Dtos.Account;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Repositories;

namespace poscam.AuthServer.Services;

/// <summary>
/// 관리자/담당자 계정 서비스.
/// 
/// 회원가입, 로그인, 승인, 차단 처리를 담당한다.
/// 관리자/담당자 로그인 토큰은 AccountTokenService를 통해 발급한다.
/// </summary>
public class AccountService
{
    private readonly UserAccountRepository _userAccountRepository;
    private readonly PartnerRepository _partnerRepository;
    private readonly PasswordHashService _passwordHashService;
    private readonly AccountTokenService _accountTokenService;

    public AccountService(
        UserAccountRepository userAccountRepository,
        PartnerRepository partnerRepository,
        PasswordHashService passwordHashService,
        AccountTokenService accountTokenService)
    {
        _userAccountRepository = userAccountRepository;
        _partnerRepository = partnerRepository;
        _passwordHashService = passwordHashService;
        _accountTokenService = accountTokenService;
    }

    /// <summary>
    /// 담당자 회원가입.
    /// 
    /// 담당자는 기본적으로 승인대기 상태로 등록된다.
    /// 관리자가 승인해야 정상 로그인할 수 있다.
    /// </summary>
    public async Task<ApiResponse<UserRegisterResponse>> RegisterAsync(UserRegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return ApiResponse<UserRegisterResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 ID를 입력해야 합니다.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return ApiResponse<UserRegisterResponse>.Fail(
                AuthErrorCode.InvalidPassword,
                "비밀번호를 입력해야 합니다.");
        }

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            return ApiResponse<UserRegisterResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "담당자명을 입력해야 합니다.");
        }

        var userId = request.UserId.Trim();

        var exists = await _userAccountRepository.ExistsUserIdAsync(userId);

        if (exists)
        {
            return ApiResponse<UserRegisterResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "이미 사용 중인 로그인 ID입니다.");
        }

        if (request.PartnerCode != null)
        {
            var partner = await _partnerRepository.GetByCodeAsync(request.PartnerCode.Value);

            if (partner == null)
            {
                return ApiResponse<UserRegisterResponse>.Fail(
                    AuthErrorCode.InvalidStore,
                    "파트너사 정보를 찾을 수 없습니다.");
            }

            if (partner.PartnerStatus != (int)PartnerStatus.Active)
            {
                return ApiResponse<UserRegisterResponse>.Fail(
                    AuthErrorCode.InvalidStore,
                    "사용할 수 없는 파트너사입니다.");
            }
        }

        var passwordHash = _passwordHashService.HashPassword(request.Password);

        var user = new UserAccount
        {
            PartnerCode = request.PartnerCode,
            UserId = userId,
            UserPasswordHash = passwordHash,
            UserName = request.UserName.Trim(),
            UserCell = request.UserCell?.Trim(),
            UserEmail = request.UserEmail?.Trim(),
            UserRole = (int)UserRole.PartnerUser,
            UserStatus = (int)UserStatus.Pending
        };

        var userCode = await _userAccountRepository.InsertAsync(user);

        var response = new UserRegisterResponse
        {
            UserCode = userCode,
            UserId = user.UserId,
            UserName = user.UserName,
            IsPendingApproval = true
        };

        return ApiResponse<UserRegisterResponse>.Ok(
            response,
            "회원가입이 완료되었습니다. 관리자 승인 후 사용할 수 있습니다.");
    }

    /// <summary>
    /// 관리자/담당자 로그인.
    /// 
    /// 정상 승인된 계정만 로그인 가능하다.
    /// 로그인 성공 시 관리자 웹 API 호출용 토큰을 발급한다.
    /// </summary>
    public async Task<ApiResponse<UserLoginResponse>> LoginAsync(UserLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return ApiResponse<UserLoginResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 ID를 입력해야 합니다.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return ApiResponse<UserLoginResponse>.Fail(
                AuthErrorCode.InvalidPassword,
                "비밀번호를 입력해야 합니다.");
        }

        var user = await _userAccountRepository.GetByUserIdAsync(request.UserId.Trim());

        if (user == null)
        {
            return ApiResponse<UserLoginResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 올바르지 않습니다.");
        }

        var passwordValid = _passwordHashService.VerifyPassword(
            request.Password,
            user.UserPasswordHash);

        if (!passwordValid)
        {
            return ApiResponse<UserLoginResponse>.Fail(
                AuthErrorCode.InvalidPassword,
                "비밀번호가 올바르지 않습니다.");
        }

        if (user.UserStatus == (int)UserStatus.Pending)
        {
            return ApiResponse<UserLoginResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "관리자 승인 대기 중인 계정입니다.");
        }

        if (user.UserStatus == (int)UserStatus.Suspended)
        {
            return ApiResponse<UserLoginResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "일시중지된 계정입니다.");
        }

        if (user.UserStatus == (int)UserStatus.Blocked)
        {
            return ApiResponse<UserLoginResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "차단된 계정입니다.");
        }

        if (user.UserStatus != (int)UserStatus.Active)
        {
            return ApiResponse<UserLoginResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "사용할 수 없는 계정 상태입니다.");
        }

        if (user.UserRole != (int)UserRole.System &&
            user.UserRole != (int)UserRole.Admin &&
            user.UserRole != (int)UserRole.PartnerUser)
        {
            return ApiResponse<UserLoginResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "사용자 권한이 올바르지 않습니다.");
        }

        var token = _accountTokenService.CreateToken(user);

        var response = new UserLoginResponse
        {
            UserCode = user.UserCode,
            PartnerCode = user.PartnerCode,
            UserId = user.UserId,
            UserName = user.UserName,
            UserRole = user.UserRole,
            UserStatus = user.UserStatus,
            Token = token
        };

        return ApiResponse<UserLoginResponse>.Ok(
            response,
            "로그인되었습니다.");
    }

    /// <summary>
    /// 승인 대기 담당자 목록 조회.
    /// 
    /// 관리자 화면에서 사용한다.
    /// </summary>
    public async Task<ApiResponse<List<UserPendingListItemDto>>> GetPendingUsersAsync()
    {
        var users = await _userAccountRepository.GetPendingUsersAsync();

        return ApiResponse<List<UserPendingListItemDto>>.Ok(
            users,
            "승인 대기 사용자 목록을 조회했습니다.");
    }

    /// <summary>
    /// 담당자 승인.
    /// 
    /// 승인자는 반드시 정상 상태의 관리자여야 한다.
    /// </summary>
    public async Task<ApiResponse<UserApproveResponse>> ApproveAsync(UserApproveRequest request)
    {
        if (request.UserCode <= 0)
        {
            return ApiResponse<UserApproveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "승인할 사용자 코드가 올바르지 않습니다.");
        }

        if (request.ApprovedBy <= 0)
        {
            return ApiResponse<UserApproveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "승인자 코드가 올바르지 않습니다.");
        }

        var approver = await _userAccountRepository.GetByCodeAsync(request.ApprovedBy);

        if (approver == null)
        {
            return ApiResponse<UserApproveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "승인자 정보를 찾을 수 없습니다.");
        }

        if (approver.UserRole != (int)UserRole.Admin ||
            approver.UserStatus != (int)UserStatus.Active)
        {
            return ApiResponse<UserApproveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "승인 권한이 없습니다.");
        }

        var targetUser = await _userAccountRepository.GetByCodeAsync(request.UserCode);

        if (targetUser == null)
        {
            return ApiResponse<UserApproveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "승인할 사용자 정보를 찾을 수 없습니다.");
        }

        if (targetUser.UserStatus == (int)UserStatus.Active)
        {
            return ApiResponse<UserApproveResponse>.Ok(
                new UserApproveResponse
                {
                    UserCode = targetUser.UserCode,
                    Approved = true,
                    ApprovedAt = targetUser.ApprovedAt ?? DateTime.UtcNow
                },
                "이미 승인된 사용자입니다.");
        }

        if (targetUser.UserStatus == (int)UserStatus.Blocked)
        {
            return ApiResponse<UserApproveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "차단된 사용자는 승인할 수 없습니다.");
        }

        var affected = await _userAccountRepository.ApproveAsync(
            request.UserCode,
            request.ApprovedBy);

        if (affected <= 0)
        {
            return ApiResponse<UserApproveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "사용자 승인이 처리되지 않았습니다.");
        }

        var response = new UserApproveResponse
        {
            UserCode = request.UserCode,
            Approved = true,
            ApprovedAt = DateTime.UtcNow
        };

        return ApiResponse<UserApproveResponse>.Ok(
            response,
            "사용자가 승인되었습니다.");
    }

    /// <summary>
    /// 사용자 차단.
    /// 
    /// 차단된 사용자는 로그인할 수 없다.
    /// 기존 토큰이 남아 있어도 추후 API 권한 검증 시 차단된다.
    /// </summary>
    public async Task<ApiResponse<bool>> BlockAsync(int userCode)
    {
        if (userCode <= 0)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "사용자 코드가 올바르지 않습니다.");
        }

        var user = await _userAccountRepository.GetByCodeAsync(userCode);

        if (user == null)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "사용자 정보를 찾을 수 없습니다.");
        }

        if (user.UserRole == (int)UserRole.Admin)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "관리자 계정은 이 기능으로 차단할 수 없습니다.");
        }

        var affected = await _userAccountRepository.BlockAsync(userCode);

        if (affected <= 0)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "사용자 차단이 처리되지 않았습니다.");
        }

        return ApiResponse<bool>.Ok(
            true,
            "사용자가 차단되었습니다.");
    }

    /// <summary>
    /// 사용자 일시중지.
    /// 
    /// 일시중지된 사용자는 로그인할 수 없다.
    /// </summary>
    public async Task<ApiResponse<bool>> SuspendAsync(int userCode)
    {
        if (userCode <= 0)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "사용자 코드가 올바르지 않습니다.");
        }

        var user = await _userAccountRepository.GetByCodeAsync(userCode);

        if (user == null)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "사용자 정보를 찾을 수 없습니다.");
        }

        if (user.UserRole == (int)UserRole.Admin)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "관리자 계정은 이 기능으로 일시중지할 수 없습니다.");
        }

        var affected = await _userAccountRepository.SuspendAsync(userCode);

        if (affected <= 0)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "사용자 일시중지가 처리되지 않았습니다.");
        }

        return ApiResponse<bool>.Ok(
            true,
            "사용자가 일시중지되었습니다.");
    }

    /// <summary>
    /// 토큰을 검증하고 현재 사용 가능한 로그인 사용자 정보를 반환한다.
    /// 
    /// 이 메서드는 나중에 StoreManageController 등에서 공통으로 사용한다.
    /// 토큰이 유효하더라도 users 테이블을 다시 조회하여 현재 상태를 확인한다.
    /// </summary>
    public async Task<ApiResponse<UserAccount>> GetLoginUserByTokenAsync(string? authorizationHeader)
    {
        var token = _accountTokenService.ExtractBearerToken(authorizationHeader);

        if (string.IsNullOrWhiteSpace(token))
        {
            return ApiResponse<UserAccount>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 토큰이 없습니다.");
        }

        var validation = _accountTokenService.ValidateToken(token);

        if (!validation.IsValid || validation.Payload == null)
        {
            return ApiResponse<UserAccount>.Fail(
                validation.ErrorCode,
                validation.Message);
        }

        var user = await _userAccountRepository.GetByCodeAsync(validation.Payload.UserCode);

        if (user == null)
        {
            return ApiResponse<UserAccount>.Fail(
                AuthErrorCode.InvalidLogin,
                "사용자 정보를 찾을 수 없습니다.");
        }

        if (user.UserStatus != (int)UserStatus.Active)
        {
            return ApiResponse<UserAccount>.Fail(
                AuthErrorCode.InvalidLogin,
                "현재 사용할 수 없는 계정입니다.");
        }

        if (user.UserRole != (int)UserRole.System &&
    user.UserRole != (int)UserRole.Admin &&
    user.UserRole != (int)UserRole.PartnerUser)
        {
            return ApiResponse<UserAccount>.Fail(
                AuthErrorCode.InvalidLogin,
                "사용자 권한이 올바르지 않습니다.");
        }

        return ApiResponse<UserAccount>.Ok(
            user,
            "로그인 사용자 정보가 확인되었습니다.");
    }

    /// <summary>
    /// 활성 담당자 목록 조회.
    /// 매장 담당자 배정 화면에서 사용한다.
    /// </summary>
    public async Task<ApiResponse<List<UserListItemDto>>> GetActivePartnerUsersAsync()
    {
        var users = await _userAccountRepository.GetActivePartnerUsersAsync();

        return ApiResponse<List<UserListItemDto>>.Ok(
            users,
            "활성 담당자 목록을 조회했습니다.");
    }

    /// <summary>
    /// 특정 파트너사 내 활성 담당자 목록 조회.
    /// 
    /// 관리자:
    /// - 모든 파트너사 담당자 조회 가능
    /// 
    /// 담당자:
    /// - 본인 소속 파트너사의 담당자만 조회 가능
    /// </summary>
    public async Task<ApiResponse<List<UserListItemDto>>> GetActivePartnerUsersByPartnerAsync(
    int partnerCode,
    UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return ApiResponse<List<UserListItemDto>>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        if (partnerCode <= 0)
        {
            return ApiResponse<List<UserListItemDto>>.Fail(
                AuthErrorCode.InvalidStore,
                "파트너사 코드가 올바르지 않습니다.");
        }

        var loginUserRole = (UserRole)loginUser.UserRole;

        if (loginUserRole == UserRole.PartnerUser)
        {
            if (loginUser.PartnerCode == null ||
                loginUser.PartnerCode.Value != partnerCode)
            {
                return ApiResponse<List<UserListItemDto>>.Fail(
                    AuthErrorCode.PermissionDenied,
                    "본인 파트너사의 담당자만 조회할 수 있습니다.");
            }
        }
        else if (
            loginUserRole != UserRole.System &&
            loginUserRole != UserRole.Admin)
        {
            return ApiResponse<List<UserListItemDto>>.Fail(
                AuthErrorCode.PermissionDenied,
                "담당자 목록을 조회할 권한이 없습니다.");
        }

        var users = await _userAccountRepository
            .GetActivePartnerUsersByPartnerAsync(partnerCode);

        return ApiResponse<List<UserListItemDto>>.Ok(
            users,
            "파트너사 담당자 목록을 조회했습니다.");
    }
}