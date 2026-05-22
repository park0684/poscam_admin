using poscam.AuthServer.Models.Dtos.Admin;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.License;
using poscam.AuthServer.Models.Dtos.Store;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Repositories;

namespace poscam.AuthServer.Services;

/// <summary>
/// 관리자/담당자용 라이선스 관리 서비스.
/// 
/// 라이선스 목록 조회와 PC캠 라이선스 발급을 담당한다.
/// System / 관리자는 전체 계약과 매장의 라이선스를 관리할 수 있고,
/// 담당자는 본인 소속 파트너사에 연결된 매장 및 계약 범위 내에서
/// 라이선스 조회와 발급 처리를 수행할 수 있다.
/// </summary>
public class LicenseManageService
{
    private readonly ContractRepository _contractRepository;
    private readonly LicenseKeyRepository _licenseKeyRepository;
    private readonly AdminService _adminService;
    private readonly IDbContext _dbContext;
    private readonly LicenseLogRepository _licenseLogRepository;
    private readonly DeviceRepository _deviceRepository;
    private readonly CodeGenerateService _codeGenerateService;
    private readonly StoreRepository _storeRepository;
    private readonly AdminPermissionService _adminPermissionService;

    public LicenseManageService(
    IDbContext dbContext,
    ContractRepository contractRepository,
    LicenseKeyRepository licenseKeyRepository,
    LicenseLogRepository licenseLogRepository,
    DeviceRepository deviceRepository,
    CodeGenerateService codeGenerateService,
    AdminService adminService,
    StoreRepository storeRepository,
    AdminPermissionService adminPermissionService)
    {
        _dbContext = dbContext;
        _contractRepository = contractRepository;
        _licenseKeyRepository = licenseKeyRepository;
        _licenseLogRepository = licenseLogRepository;
        _deviceRepository = deviceRepository;
        _codeGenerateService = codeGenerateService;
        _adminService = adminService;
        _storeRepository = storeRepository;
        _adminPermissionService = adminPermissionService;
    }

    /// <summary>
    /// 매장 기준 라이선스 목록을 조회한다.
    /// 
    /// 권한 정책:
    /// - System: 허용
    /// - Admin: LicenseManage 권한이 있어야 허용
    /// - PartnerUser: 본인 소속 파트너사에 연결된 매장의 라이선스만 조회 가능
    /// </summary>
    public async Task<ApiResponse<List<StoreLicenseDto>>> GetLicensesByStoreAsync(
        int storeCode,
        UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return ApiResponse<List<StoreLicenseDto>>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        if (storeCode <= 0)
        {
            return ApiResponse<List<StoreLicenseDto>>.Fail(
                AuthErrorCode.InvalidStore,
                "매장 코드가 올바르지 않습니다.");
        }

        var loginUserRole = (UserRole)loginUser.UserRole;

        // System / Admin은 라이선스 관리 권한 확인
        if (loginUserRole == UserRole.System ||
            loginUserRole == UserRole.Admin)
        {
            var permissionResult = await CheckLicenseManagePermissionAsync(
                loginUser);

            if (!permissionResult.Success)
            {
                return ApiResponse<List<StoreLicenseDto>>.Fail(
                    permissionResult.ErrorCode,
                    permissionResult.Message);
            }
        }
        else if (loginUserRole != UserRole.PartnerUser)
        {
            return ApiResponse<List<StoreLicenseDto>>.Fail(
                AuthErrorCode.PermissionDenied,
                "라이선스 정보를 조회할 권한이 없습니다.");
        }

        var canAccess = await CanAccessStoreAsync(
            storeCode,
            loginUser);

        if (!canAccess)
        {
            return ApiResponse<List<StoreLicenseDto>>.Fail(
                AuthErrorCode.PermissionDenied,
                "해당 매장의 라이선스 정보를 조회할 권한이 없습니다.");
        }

        var licenses = await _licenseKeyRepository.GetByStoreAsync(storeCode);

        return ApiResponse<List<StoreLicenseDto>>.Ok(
            licenses,
            "매장 라이선스 목록을 조회했습니다.");
    }

    /// <summary>
    /// 계약 기준 라이선스 목록을 조회한다.
    /// 
    /// 권한 정책:
    /// - System: 허용
    /// - Admin: LicenseManage 권한이 있어야 허용
    /// - PartnerUser: 본인 소속 파트너사의 계약에 속한 라이선스만 조회 가능
    /// </summary>
    public async Task<ApiResponse<List<StoreLicenseDto>>> GetLicensesByContractAsync(
        int contractCode,
        UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return ApiResponse<List<StoreLicenseDto>>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        if (contractCode <= 0)
        {
            return ApiResponse<List<StoreLicenseDto>>.Fail(
                AuthErrorCode.ContractNotFound,
                "계약 코드가 올바르지 않습니다.");
        }

        var loginUserRole = (UserRole)loginUser.UserRole;

        // System / Admin은 라이선스 관리 권한 확인
        if (loginUserRole == UserRole.System ||
            loginUserRole == UserRole.Admin)
        {
            var permissionResult = await CheckLicenseManagePermissionAsync(
                loginUser);

            if (!permissionResult.Success)
            {
                return ApiResponse<List<StoreLicenseDto>>.Fail(
                    permissionResult.ErrorCode,
                    permissionResult.Message);
            }
        }
        else if (loginUserRole != UserRole.PartnerUser)
        {
            return ApiResponse<List<StoreLicenseDto>>.Fail(
                AuthErrorCode.PermissionDenied,
                "라이선스 정보를 조회할 권한이 없습니다.");
        }

        var contract = await _contractRepository.GetByCodeAsync(contractCode);

        if (contract == null)
        {
            return ApiResponse<List<StoreLicenseDto>>.Fail(
                AuthErrorCode.ContractNotFound,
                "계약 정보를 찾을 수 없습니다.");
        }

        var canAccess = CanAccessContractAsync(
            contract,
            loginUser);

        if (!canAccess)
        {
            return ApiResponse<List<StoreLicenseDto>>.Fail(
                AuthErrorCode.PermissionDenied,
                "해당 계약의 라이선스 정보를 조회할 권한이 없습니다.");
        }

        var licenses = await _licenseKeyRepository.GetByContractAsync(contractCode);

        return ApiResponse<List<StoreLicenseDto>>.Ok(
            licenses,
            "계약 라이선스 목록을 조회했습니다.");
    }

    /// <summary>
    /// 계약 기준 PC캠 라이선스를 발급한다.
    /// 
    /// 권한 정책:
    /// - System: 허용
    /// - Admin: LicenseManage 권한이 있어야 허용
    /// - PartnerUser: 본인 소속 파트너사의 계약에 대해서만 발급 가능
    /// 
    /// 실제 발급 로직은 기존 AdminService의 IssuePccamLicensesAsync를 재사용한다.
    /// </summary>
    public async Task<ApiResponse<LicenseIssueResponse>> IssueLicensesAsync(
        int contractCode,
        LicenseIssueManageRequest request,
        UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return ApiResponse<LicenseIssueResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        if (contractCode <= 0)
        {
            return ApiResponse<LicenseIssueResponse>.Fail(
                AuthErrorCode.ContractNotFound,
                "계약 코드가 올바르지 않습니다.");
        }

        if (request.Count <= 0)
        {
            return ApiResponse<LicenseIssueResponse>.Fail(
                AuthErrorCode.ContractSlotExceeded,
                "발급 수량은 1개 이상이어야 합니다.");
        }

        var loginUserRole = (UserRole)loginUser.UserRole;

        // System / Admin은 라이선스 관리 권한 확인
        if (loginUserRole == UserRole.System ||
            loginUserRole == UserRole.Admin)
        {
            var permissionResult = await CheckLicenseManagePermissionAsync(
                loginUser);

            if (!permissionResult.Success)
            {
                return ApiResponse<LicenseIssueResponse>.Fail(
                    permissionResult.ErrorCode,
                    permissionResult.Message);
            }
        }
        else if (loginUserRole != UserRole.PartnerUser)
        {
            return ApiResponse<LicenseIssueResponse>.Fail(
                AuthErrorCode.PermissionDenied,
                "라이선스를 발급할 권한이 없습니다.");
        }

        var contract = await _contractRepository.GetByCodeAsync(contractCode);

        if (contract == null)
        {
            return ApiResponse<LicenseIssueResponse>.Fail(
                AuthErrorCode.ContractNotFound,
                "계약 정보를 찾을 수 없습니다.");
        }

        var canAccess = CanAccessContractAsync(
            contract,
            loginUser);

        if (!canAccess)
        {
            return ApiResponse<LicenseIssueResponse>.Fail(
                AuthErrorCode.PermissionDenied,
                "해당 계약의 라이선스를 발급할 권한이 없습니다.");
        }

        var issueRequest = new LicenseIssueRequest
        {
            ContractCode = contractCode,
            Count = request.Count
        };

        return await _adminService.IssuePccamLicensesAsync(issueRequest);
    }


    /// <summary>
    /// 로그인 사용자가 특정 매장에 접근 가능한지 확인한다.
    /// 
    /// System / 관리자는 모든 매장에 접근할 수 있고,
    /// 담당자는 본인 소속 파트너사에 연결된 매장만 접근할 수 있다.
    /// </summary>
    private async Task<bool> CanAccessStoreAsync(
        int storeCode,
        UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return false;
        }

        var loginUserRole = (UserRole)loginUser.UserRole;

        // System / 관리자는 전체 매장 접근 가능
        if (loginUserRole == UserRole.System ||
            loginUserRole == UserRole.Admin)
        {
            return true;
        }

        // 담당자는 본인 소속 파트너사에 연결된 매장만 접근 가능
        if (loginUserRole == UserRole.PartnerUser)
        {
            if (loginUser.PartnerCode == null ||
                loginUser.PartnerCode <= 0)
            {
                return false;
            }

            return await _storeRepository.CanPartnerAccessStoreAsync(
                loginUser.PartnerCode.Value,
                storeCode);
        }

        return false;
    }

    ///// <summary>
    ///// 로그인 사용자가 특정 매장에 접근 가능한지 확인한다.
    /// 2026-05-19 제거
    ///// </summary>
    //private async Task<bool> CanAccessStoreAsync(int storeCode, UserAccount loginUser)
    //{
    //    if (loginUser.UserRole == (int)UserRole.Admin)
    //    {
    //        return true;
    //    }

    //    return await _storeAssignmentRepository.CanAccessStoreAsync(
    //        loginUser.UserCode,
    //        storeCode);
    //}

    /// <summary>
    /// 로그인 사용자가 특정 계약에 접근 가능한지 확인한다.
    /// 
    /// 계약은 파트너사 기준으로 관리된다.
    /// 
    /// System / 관리자는 모든 계약에 접근할 수 있고,
    /// 담당자는 본인 소속 파트너사의 계약만 접근할 수 있다.
    /// </summary>
    private bool CanAccessContractAsync(
        Contract contract,
        UserAccount loginUser)
    {
        if (contract == null || loginUser == null)
        {
            return false;
        }

        var loginUserRole = (UserRole)loginUser.UserRole;

        // System / 관리자는 전체 계약 접근 가능
        if (loginUserRole == UserRole.System ||
            loginUserRole == UserRole.Admin)
        {
            return true;
        }

        // 담당자는 본인 소속 파트너사의 계약만 접근 가능
        if (loginUserRole == UserRole.PartnerUser)
        {
            return loginUser.PartnerCode.HasValue &&
                   loginUser.PartnerCode.Value == contract.ConPartner;
        }

        return false;
    }

    /// <summary>
    /// 특정 인증키를 폐기한다.
    /// 
    /// 정책:
    /// - System은 모든 인증키 처리 가능
    ///- Admin은 LicenseManage 권한이 있는 경우 처리 가능
    ///- 파트너 담당자는 본인 소속 파트너사의 계약에 속한 인증키만 처리 가능
    /// - 이미 폐기된 인증키는 다시 폐기하지 않는다.
    /// - 폐기 처리 시 licensekeys.lic_status를 Revoked로 변경한다.
    /// - licenselog에 Revoke 이력을 남긴다.
    /// - 연결 장비가 있더라도 devices는 삭제하지 않는다.
    ///   실행 인증 단계에서 Revoked 상태로 차단된다.
    /// </summary>
    public async Task<ApiResponse<LicenseRevokeResponse>> RevokeLicenseAsync(
        int licenseCode,
        LicenseRevokeManageRequest request,
        UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return ApiResponse<LicenseRevokeResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        var loginUserRole = (UserRole)loginUser.UserRole;

        if (loginUserRole == UserRole.System ||
            loginUserRole == UserRole.Admin)
        {
            var permissionResult = await CheckLicenseManagePermissionAsync(
                loginUser);

            if (!permissionResult.Success)
            {
                return ApiResponse<LicenseRevokeResponse>.Fail(
                    permissionResult.ErrorCode,
                    permissionResult.Message);
            }
        }
        else if (loginUserRole != UserRole.PartnerUser)
        {
            return ApiResponse<LicenseRevokeResponse>.Fail(
                AuthErrorCode.PermissionDenied,
                "인증키를 폐기할 권한이 없습니다.");
        }

        if (licenseCode <= 0)
        {
            return ApiResponse<LicenseRevokeResponse>.Fail(
                AuthErrorCode.LicenseNotFound,
                "라이선스 코드가 올바르지 않습니다.");
        }

        using var connection = _dbContext.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            // 1. 라이선스 조회
            var license = await _licenseKeyRepository.GetByCodeAsync(
                connection,
                transaction,
                licenseCode);

            if (license == null)
            {
                transaction.Rollback();

                return ApiResponse<LicenseRevokeResponse>.Fail(
                    AuthErrorCode.LicenseNotFound,
                    "인증키를 찾을 수 없습니다.");
            }

            // 2. 이미 폐기된 인증키인지 확인
            if (license.LicStatus == (int)LicenseStatus.Revoked)
            {
                transaction.Rollback();

                return ApiResponse<LicenseRevokeResponse>.Fail(
                    AuthErrorCode.LicenseRevoked,
                    "이미 폐기된 인증키입니다.");
            }

            // 3. 계약 조회
            var contract = await _contractRepository.GetByCodeAsync(
                connection,
                transaction,
                license.LicContract);

            if (contract == null)
            {
                transaction.Rollback();

                return ApiResponse<LicenseRevokeResponse>.Fail(
                    AuthErrorCode.ContractNotFound,
                    "라이선스에 연결된 계약 정보를 찾을 수 없습니다.");
            }

            // 4. 계약 접근 권한 확인
            var canAccess = CanAccessContractAsync(contract, loginUser);

            if (!canAccess)
            {
                transaction.Rollback();

                return ApiResponse<LicenseRevokeResponse>.Fail(
                    AuthErrorCode.PermissionDenied,
                    "해당 인증키를 폐기할 권한이 없습니다.");
            }

            // 5. 연결 장비가 있다면 HWID를 로그에 함께 남긴다.
            var device = await _deviceRepository.FindByLicenseAsync(
                connection,
                transaction,
                license.LicCode);

            // 6. 라이선스 상태를 Revoked로 변경
            await _licenseKeyRepository.UpdateStatusAsync(
                connection,
                transaction,
                license.LicCode,
                (int)LicenseStatus.Revoked);

            // 7. 폐기 로그 저장
            await _licenseLogRepository.InsertAsync(
                connection,
                transaction,
                new LicenseLog
                {
                    LigCode = _codeGenerateService.CreateLicenseLogCode(),
                    LigLicense = license.LicCode,
                    LigStore = contract.ConStore,
                    LigHwid = device?.DevHwid ?? "",
                    LigActionType = (int)LicenseActionType.Revoke,
                    LigReason = string.IsNullOrWhiteSpace(request.Reason)
                        ? "인증키 폐기"
                        : request.Reason.Trim()
                });

            transaction.Commit();

            return ApiResponse<LicenseRevokeResponse>.Ok(
                new LicenseRevokeResponse
                {
                    LicenseCode = license.LicCode,
                    ContractCode = contract.ConCode,
                    StoreCode = contract.ConStore,
                    Revoked = true
                },
                "인증키가 폐기되었습니다.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 폐기된 인증키를 복구한다.
    /// 
    /// 정책:
    /// -  System은 모든 인증키 처리 가능
    /// - Admin은 LicenseManage 권한이 있는 경우 처리 가능
    /// - 파트너 담당자는 본인 소속 파트너사의 계약에 속한 인증키만 복구 가능
    /// - 폐기 상태인 인증키만 복구할 수 있다.
    /// - 연결된 디바이스가 있으면 Activated(사용중)로 복구한다.
    /// - 연결된 디바이스가 없으면 Reset(초기화)로 복구한다.
    /// - licenselog에 Restore 이력을 남긴다.
    /// </summary>
    public async Task<ApiResponse<LicenseRestoreResponse>> RestoreLicenseAsync(
        int licenseCode,
        LicenseRestoreManageRequest request,
        UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return ApiResponse<LicenseRestoreResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        var loginUserRole = (UserRole)loginUser.UserRole;

        if (loginUserRole == UserRole.System ||
            loginUserRole == UserRole.Admin)
        {
            var permissionResult = await CheckLicenseManagePermissionAsync(
                loginUser);

            if (!permissionResult.Success)
            {
                return ApiResponse<LicenseRestoreResponse>.Fail(
                    permissionResult.ErrorCode,
                    permissionResult.Message);
            }
        }
        else if (loginUserRole != UserRole.PartnerUser)
        {
            return ApiResponse<LicenseRestoreResponse>.Fail(
                AuthErrorCode.PermissionDenied,
                "인증키를 복구할 권한이 없습니다.");
        }

        if (licenseCode <= 0)
        {
            return ApiResponse<LicenseRestoreResponse>.Fail(
                AuthErrorCode.LicenseNotFound,
                "라이선스 코드가 올바르지 않습니다.");
        }

        using var connection = _dbContext.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            // 1. 라이선스 조회
            var license = await _licenseKeyRepository.GetByCodeAsync(
                connection,
                transaction,
                licenseCode);

            if (license == null)
            {
                transaction.Rollback();

                return ApiResponse<LicenseRestoreResponse>.Fail(
                    AuthErrorCode.LicenseNotFound,
                    "인증키를 찾을 수 없습니다.");
            }

            // 2. 폐기 상태인 인증키만 복구 가능
            if (license.LicStatus != (int)LicenseStatus.Revoked)
            {
                transaction.Rollback();

                return ApiResponse<LicenseRestoreResponse>.Fail(
                    AuthErrorCode.ValidationError,
                    "폐기 상태의 인증키만 복구할 수 있습니다.");
            }

            // 3. 계약 조회
            var contract = await _contractRepository.GetByCodeAsync(
                connection,
                transaction,
                license.LicContract);

            if (contract == null)
            {
                transaction.Rollback();

                return ApiResponse<LicenseRestoreResponse>.Fail(
                    AuthErrorCode.ContractNotFound,
                    "라이선스에 연결된 계약 정보를 찾을 수 없습니다.");
            }

            // 4. 계약 접근 권한 확인
            var canAccess = CanAccessContractAsync(contract, loginUser);

            if (!canAccess)
            {
                transaction.Rollback();

                return ApiResponse<LicenseRestoreResponse>.Fail(
                    AuthErrorCode.PermissionDenied,
                    "해당 인증키를 복구할 권한이 없습니다.");
            }

            // 5. 연결 장비 확인
            var device = await _deviceRepository.FindByLicenseAsync(
                connection,
                transaction,
                license.LicCode);

            // 장비가 있으면 사용중, 없으면 초기화
            var restoredStatus = device != null
                ? (int)LicenseStatus.Activated
                : (int)LicenseStatus.Reset;

            // 6. 라이선스 상태 복구
            await _licenseKeyRepository.UpdateStatusAsync(
                connection,
                transaction,
                license.LicCode,
                restoredStatus);

            // 7. 복구 로그 저장
            await _licenseLogRepository.InsertAsync(
                connection,
                transaction,
                new LicenseLog
                {
                    LigCode = _codeGenerateService.CreateLicenseLogCode(),
                    LigLicense = license.LicCode,
                    LigStore = contract.ConStore,
                    LigHwid = device?.DevHwid ?? "",
                    LigActionType = (int)LicenseActionType.Restore,
                    LigReason = string.IsNullOrWhiteSpace(request.Reason)
                        ? "인증키 복구"
                        : request.Reason.Trim()
                });

            transaction.Commit();

            return ApiResponse<LicenseRestoreResponse>.Ok(
                new LicenseRestoreResponse
                {
                    LicenseCode = license.LicCode,
                    ContractCode = contract.ConCode,
                    StoreCode = contract.ConStore,
                    RestoredStatus = restoredStatus,
                    Restored = true
                },
                device != null
                    ? "인증키가 복구되었습니다. 연결 장비가 있어 사용중 상태로 변경되었습니다."
                    : "인증키가 복구되었습니다. 연결 장비가 없어 초기화 상태로 변경되었습니다.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 라이선스 관리 권한을 확인한다.
    /// 
    /// System은 자동 허용되고,
    /// Admin은 LicenseManage 권한을 보유해야 한다.
    /// </summary>
    private Task<ApiResponse<bool>> CheckLicenseManagePermissionAsync(
        UserAccount loginUser)
    {
        return _adminPermissionService.CheckPermissionAsync(
            loginUser,
            AdminPermissionType.LicenseManage);
    }
}