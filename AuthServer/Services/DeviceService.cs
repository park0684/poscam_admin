using System.Text.Json;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.Device;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Repositories;

namespace poscam.AuthServer.Services;

/// <summary>
/// 관리자 장비 관리 서비스.
/// 
/// PC캠 장비 초기화, 캠뷰어 장비 초기화, 장비 삭제를 담당한다.
/// PC캠 장비 초기화 시에는 연결된 라이선스 상태를 Reset으로 되돌린다.
/// </summary>
public class DeviceService
{
    private readonly IDbContext _dbContext;
    private readonly DeviceRepository _deviceRepository;
    private readonly LicenseKeyRepository _licenseKeyRepository;
    private readonly AuthLogRepository _authLogRepository;
    private readonly LicenseLogRepository _licenseLogRepository;
    private readonly CodeGenerateService _codeGenerateService;
    private readonly StoreAssignmentRepository _storeAssignmentRepository;
    private readonly ContractRepository _contractRepository;
    public DeviceService(
        IDbContext dbContext,
        DeviceRepository deviceRepository,
        LicenseKeyRepository licenseKeyRepository,
        AuthLogRepository authLogRepository,
        LicenseLogRepository licenseLogRepository,
        CodeGenerateService codeGenerateService,
        StoreAssignmentRepository storeAssignmentRepository,
        ContractRepository contractRepository)
    {
        _dbContext = dbContext;
        _deviceRepository = deviceRepository;
        _licenseKeyRepository = licenseKeyRepository;
        _authLogRepository = authLogRepository;
        _licenseLogRepository = licenseLogRepository;
        _codeGenerateService = codeGenerateService;
        _storeAssignmentRepository = storeAssignmentRepository;
        _contractRepository = contractRepository;
    }

    /// <summary>
    /// 장비 초기화.
    /// 
    /// 관리자:
    /// - 모든 매장 장비 초기화 가능
    /// 
    /// 담당자:
    /// - 본인에게 배정된 매장의 장비만 초기화 가능
    /// 
    /// PC캠 장비:
    /// - devices에서 장비 삭제
    /// - 연결된 licensekeys.lic_status를 Reset으로 변경
    /// - licenselog 기록
    /// 
    /// 캠뷰어 장비:
    /// - devices에서 장비 삭제
    /// - auth_logs 기록
    /// </summary>
    public async Task<ApiResponse<DeviceResetResponse>> ResetDeviceAsync(
    DeviceResetRequest request,
    UserAccount loginUser,
    string? requestIp = null)
    {
        if (request.DeviceCode <= 0)
        {
            return ApiResponse<DeviceResetResponse>.Fail(
                AuthErrorCode.DeviceNotFound,
                "장비 코드가 올바르지 않습니다.");
        }

        using var connection = _dbContext.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            var device = await _deviceRepository.GetByCodeAsync(
                connection,
                transaction,
                request.DeviceCode);

            if (device == null)
            {
                transaction.Rollback();

                return ApiResponse<DeviceResetResponse>.Fail(
                    AuthErrorCode.DeviceNotFound,
                    "장비 정보를 찾을 수 없습니다.");
            }

            var isPccam = device.DevAppType == (int)DeviceAppType.Pccam;
            var isViewer = device.DevAppType == (int)DeviceAppType.Viewer;

            if (!isPccam && !isViewer)
            {
                transaction.Rollback();

                return ApiResponse<DeviceResetResponse>.Fail(
                    AuthErrorCode.DeviceNotFound,
                    "알 수 없는 장비 유형입니다.");
            }

            var canReset = await CanResetDeviceAsync(
                device,
                loginUser);

            if (!canReset)
            {
                transaction.Rollback();

                return ApiResponse<DeviceResetResponse>.Fail(
                    AuthErrorCode.InvalidStore,
                    "해당 장비를 초기화할 권한이 없습니다.");
            }

            if (isPccam)
            {
                if (device.DevLicense == null)
                {
                    transaction.Rollback();

                    return ApiResponse<DeviceResetResponse>.Fail(
                        AuthErrorCode.LicenseNotFound,
                        "PC캠 장비에 연결된 라이선스가 없습니다.");
                }

                await _licenseKeyRepository.UpdateStatusAsync(
                    connection,
                    transaction,
                    device.DevLicense.Value,
                    (int)LicenseStatus.Reset);

                await _licenseLogRepository.InsertAsync(
                    connection,
                    transaction,
                    new LicenseLog
                    {
                        LigCode = _codeGenerateService.CreateLicenseLogCode(),
                        LigLicense = device.DevLicense.Value,
                        LigStore = device.DevStore,
                        LigHwid = device.DevHwid,
                        LigActionType = (int)LicenseActionType.Reset,
                        LigReason = string.IsNullOrWhiteSpace(request.Reason)
                            ? "장비 초기화로 인한 PC캠 라이선스 재등록 가능 처리"
                            : request.Reason.Trim()
                    });
            }

            await _deviceRepository.DeleteAsync(
                connection,
                transaction,
                request.DeviceCode);

            await _authLogRepository.InsertAsync(
                connection,
                transaction,
                CreateAuthLog(
                    AuthRequestType.AdminDeviceReset,
                    device.DevStore,
                    AuthResult.Success,
                    AuthErrorCode.None,
                    requestIp,
                    new
                    {
                        request.DeviceCode,
                        device.DevStore,
                        device.DevLicense,
                        device.DevAppType,
                        device.DevHwid,
                        device.DevPos,
                        device.DevName,
                        LoginUserCode = loginUser.UserCode,
                        LoginUserRole = loginUser.UserRole,
                        UserReason = request.Reason,
                        SystemReason = isPccam
                            ? "PCCAM device reset and license status changed to Reset"
                            : "Viewer device reset"
                    }));

            transaction.Commit();

            return ApiResponse<DeviceResetResponse>.Ok(
                new DeviceResetResponse
                {
                    DeviceCode = request.DeviceCode,
                    ResetSuccess = true
                },
                isPccam
                    ? "PC캠 장비가 초기화되었습니다. 해당 라이선스는 재등록 가능한 상태로 변경되었습니다."
                    : "캠뷰어 장비가 초기화되었습니다.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 로그인 사용자가 해당 장비를 초기화할 수 있는지 확인한다.
    /// 
    /// 관리자:
    /// - 모든 장비 초기화 가능
    /// 
    /// 파트너 담당자:
    /// - 매장 연결 장비: 본인에게 배정된 매장의 장비만 초기화 가능
    /// - 매장 없는 PC캠 장비:
    ///   장비의 라이선스 → 계약을 조회하여
    ///   계약 소유 파트너사가 본인 파트너사와 같으면 초기화 가능
    /// 
    /// 캠뷰어 장비:
    /// - 반드시 매장 연결 장비여야 하므로,
    ///   매장 없는 상태라면 초기화 권한 없음
    /// </summary>
    private async Task<bool> CanResetDeviceAsync(
        Device device,
        UserAccount loginUser)
    {
        // 1. 시스템 관리자 및 관리자는 전체 장비 초기화 가능
        if (loginUser.UserRole == (int)UserRole.System ||
    loginUser.UserRole == (int)UserRole.Admin)
        {
            return true;
        }

        // 2. 파트너 담당자가 아니면 불가
        if (loginUser.UserRole != (int)UserRole.PartnerUser)
        {
            return false;
        }

        // 3. 매장 연결 장비는 기존처럼 매장 접근 권한으로 판단
        if (device.DevStore.HasValue)
        {
            return await _storeAssignmentRepository.CanAccessStoreAsync(
                loginUser.UserCode,
                device.DevStore.Value);
        }

        // 4. 매장 없는 장비는 PC캠만 허용
        if (device.DevAppType != (int)DeviceAppType.Pccam)
        {
            return false;
        }

        // 5. PC캠 장비는 반드시 라이선스를 가지고 있어야 한다.
        if (!device.DevLicense.HasValue)
        {
            return false;
        }

        // 6. 라이선스 조회
        var license = await _licenseKeyRepository.GetByCodeAsync(
            device.DevLicense.Value);

        if (license == null)
        {
            return false;
        }

        // 7. 라이선스가 속한 계약 조회
        var contract = await _contractRepository.GetByCodeAsync(
            license.LicContract);

        if (contract == null)
        {
            return false;
        }

        // 8. 본인 소속 파트너사의 계약인지 확인
        return loginUser.PartnerCode.HasValue
               && loginUser.PartnerCode.Value == contract.ConPartner;
    }

    /// <summary>
    /// 인증 로그 객체를 생성한다.
    /// 
    /// 매장 없는 PC캠 장비도 초기화될 수 있으므로
    /// storeCode는 nullable로 처리한다.
    /// </summary>
    private static AuthLog CreateAuthLog(
        AuthRequestType requestType,
        int? storeCode,
        AuthResult result,
        AuthErrorCode errorCode,
        string? requestIp,
        object details)
    {
        return new AuthLog
        {
            AlRequest = (int)requestType,
            AlStore = storeCode,
            AlResult = (int)result,
            AlError = errorCode == AuthErrorCode.None
                ? null
                : (int)errorCode,
            AlIp = requestIp,
            AlDetails = JsonSerializer.Serialize(details)
        };
    }
}