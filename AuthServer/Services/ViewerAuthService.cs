using System.Text.Json;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.Viewer;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Repositories;

namespace poscam.AuthServer.Services;

/// <summary>
/// 캠뷰어 인증 서비스.
/// 
/// 캠뷰어는 최초 실행 시 매장 ID/비밀번호로 로그인하여 토큰을 발급받고,
/// 이후 실행부터는 로컬에 저장된 토큰으로 실행 인증을 수행한다.
/// 
/// 단, 토큰이 있더라도 devices 테이블에 장비가 없으면
/// 사용 해제된 장비로 판단하고 실행을 차단한다.
/// </summary>
public class ViewerAuthService
{
    private readonly IDbContext _dbContext;
    private readonly StoreRepository _storeRepository;
    private readonly ContractRepository _contractRepository;
    private readonly DeviceRepository _deviceRepository;
    private readonly NvrConfigRepository _nvrConfigRepository;
    private readonly AuthLogRepository _authLogRepository;
    private readonly PasswordService _passwordService;
    private readonly TokenService _tokenService;

    public ViewerAuthService(
        IDbContext dbContext,
        StoreRepository storeRepository,
        ContractRepository contractRepository,
        DeviceRepository deviceRepository,
        NvrConfigRepository nvrConfigRepository,
        AuthLogRepository authLogRepository,
        PasswordService passwordService,
        TokenService tokenService)
    {
        _dbContext = dbContext;
        _storeRepository = storeRepository;
        _contractRepository = contractRepository;
        _deviceRepository = deviceRepository;
        _nvrConfigRepository = nvrConfigRepository;
        _authLogRepository = authLogRepository;
        _passwordService = passwordService;
        _tokenService = tokenService;
    }

    /// <summary>
    /// 캠뷰어 최초 로그인.
    /// 
    /// 사용자는 stores.store_code를 입력하지 않는다.
    /// 화면에서 입력하는 매장코드는 stores.store_id이며,
    /// 서버가 store_id로 매장을 조회한 뒤 내부 store_code를 사용한다.
    /// 
    /// 기존 HWID가 이미 등록되어 있으면 장비를 새로 등록하지 않고 토큰만 갱신한다.
    /// 신규 HWID이면 계약의 캠뷰어 허용 수량을 확인한 뒤 devices에 등록한다.
    /// </summary>
    public async Task<ApiResponse<ViewerLoginResponse>> LoginAsync(
        ViewerLoginRequest request,
        string? requestIp = null)
    {
        if (string.IsNullOrWhiteSpace(request.StoreId))
        {
            return ApiResponse<ViewerLoginResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "매장 ID를 입력해야 합니다.");
        }

        if (string.IsNullOrWhiteSpace(request.StorePassword))
        {
            return ApiResponse<ViewerLoginResponse>.Fail(
                AuthErrorCode.InvalidPassword,
                "비밀번호를 입력해야 합니다.");
        }

        if (string.IsNullOrWhiteSpace(request.Hwid))
        {
            return ApiResponse<ViewerLoginResponse>.Fail(
                AuthErrorCode.DuplicateHwid,
                "장비 식별값이 올바르지 않습니다.");
        }

        var storeId = request.StoreId.Trim();
        var hwid = request.Hwid.Trim();

        // 중요:
        // 사용자가 입력하는 매장코드는 stores.store_code가 아니라 stores.store_id이다.
        // store_code는 서버에서 조회된 Store 엔티티의 StoreCode를 사용한다.
        var store = await _storeRepository.GetByLoginIdAsync(storeId);

        if (store == null)
        {
            await WriteAuthLogAsync(
                AuthRequestType.ViewerLogin,
                null,
                AuthResult.Fail,
                AuthErrorCode.InvalidLogin,
                requestIp,
                new
                {
                    StoreId = storeId,
                    Hwid = hwid,
                    request.ProgramVersion,
                    reason = "Invalid store login id"
                });

            return ApiResponse<ViewerLoginResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "매장 로그인 정보가 올바르지 않습니다.");
        }

        if (!_passwordService.VerifyStorePassword(request.StorePassword, store.StorePassword))
        {
            await WriteAuthLogAsync(
                AuthRequestType.ViewerLogin,
                store.StoreCode,
                AuthResult.Fail,
                AuthErrorCode.InvalidPassword,
                requestIp,
                new
                {
                    StoreId = storeId,
                    Hwid = hwid,
                    request.ProgramVersion,
                    reason = "Invalid store password"
                });

            return ApiResponse<ViewerLoginResponse>.Fail(
                AuthErrorCode.InvalidPassword,
                "비밀번호가 올바르지 않습니다.");
        }

        if (store.StoreStatus != (int)StoreStatus.Active)
        {
            await WriteAuthLogAsync(
                AuthRequestType.ViewerLogin,
                store.StoreCode,
                AuthResult.Fail,
                AuthErrorCode.StoreInactive,
                requestIp,
                new
                {
                    StoreId = storeId,
                    Hwid = hwid,
                    request.ProgramVersion,
                    reason = "Store inactive"
                });

            return ApiResponse<ViewerLoginResponse>.Fail(
                AuthErrorCode.StoreInactive,
                "비활성 상태의 매장입니다.");
        }

        // 기존 GetValidActiveContractAsync가 아니라
        // 캠뷰어 수량이 있는 계약만 조회해야 한다.
        var activeContract = await GetValidActiveViewerContractAsync(store.StoreCode);

        if (activeContract == null)
        {
            await WriteAuthLogAsync(
                AuthRequestType.ViewerLogin,
                store.StoreCode,
                AuthResult.Fail,
                AuthErrorCode.ContractNotFound,
                requestIp,
                new
                {
                    StoreId = storeId,
                    Hwid = hwid,
                    request.ProgramVersion,
                    reason = "Valid active viewer contract not found"
                });

            return ApiResponse<ViewerLoginResponse>.Fail(
                AuthErrorCode.ContractNotFound,
                "사용 가능한 캠뷰어 계약 정보를 찾을 수 없습니다.");
        }

        var nvrConfig = await _nvrConfigRepository.GetByStoreAsync(store.StoreCode);
        var configVersion = nvrConfig?.NvrVersion ?? "";

        var existingDevice = await _deviceRepository.FindViewerByHwidAsync(
            store.StoreCode,
            hwid);

        if (existingDevice != null)
        {
            var token = _tokenService.CreateToken(
                storeCode: store.StoreCode,
                contractCode: activeContract.ConCode,
                licenseCode: null,
                deviceCode: existingDevice.DevCode,
                appType: DeviceAppType.Viewer,
                hwid: hwid,
                contractType: (ContractType)activeContract.ConType,
                isPermanent: false,
                configVersion: configVersion);

            await WriteAuthLogAsync(
                AuthRequestType.ViewerLogin,
                store.StoreCode,
                AuthResult.Success,
                AuthErrorCode.None,
                requestIp,
                new
                {
                    StoreId = storeId,
                    Hwid = hwid,
                    existingDevice.DevCode,
                    request.ProgramVersion,
                    reason = "Existing viewer device login"
                });

            return ApiResponse<ViewerLoginResponse>.Ok(
                new ViewerLoginResponse
                {
                    StoreCode = store.StoreCode,
                    DeviceCode = existingDevice.DevCode,
                    LoginSuccess = true,
                    ConfigVersion = configVersion,
                    Token = token
                },
                "캠뷰어 로그인이 완료되었습니다.");
        }

        var currentViewerCount = await _deviceRepository.CountByStoreAndAppTypeAsync(
            store.StoreCode,
            (int)DeviceAppType.Viewer);

        if (currentViewerCount >= activeContract.ConView)
        {
            await WriteAuthLogAsync(
                AuthRequestType.ViewerLogin,
                store.StoreCode,
                AuthResult.Fail,
                AuthErrorCode.DeviceLimitExceeded,
                requestIp,
                new
                {
                    StoreId = storeId,
                    Hwid = hwid,
                    request.ProgramVersion,
                    currentViewerCount,
                    allowedViewerCount = activeContract.ConView,
                    reason = "Viewer slot exceeded"
                });

            return ApiResponse<ViewerLoginResponse>.Fail(
                AuthErrorCode.DeviceLimitExceeded,
                $"캠뷰어 허용 수량({activeContract.ConView}대)을 초과했습니다.");
        }

        using var connection = _dbContext.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            var device = new Device
            {
                DevStore = store.StoreCode,
                DevLicense = null,
                DevAppType = (int)DeviceAppType.Viewer,
                DevHwid = hwid,
                DevPos = 0,
                DevName = string.IsNullOrWhiteSpace(request.DeviceName)
                    ? "캠뷰어"
                    : request.DeviceName.Trim()
            };

            var deviceCode = await _deviceRepository.InsertAsync(
                connection,
                transaction,
                device);

            var token = _tokenService.CreateToken(
                storeCode: store.StoreCode,
                contractCode: activeContract.ConCode,
                licenseCode: null,
                deviceCode: deviceCode,
                appType: DeviceAppType.Viewer,
                hwid: hwid,
                contractType: (ContractType)activeContract.ConType,
                isPermanent: false,
                configVersion: configVersion);

            await _authLogRepository.InsertAsync(
                connection,
                transaction,
                CreateAuthLog(
                    AuthRequestType.ViewerLogin,
                    store.StoreCode,
                    AuthResult.Success,
                    AuthErrorCode.None,
                    requestIp,
                    new
                    {
                        StoreId = storeId,
                        Hwid = hwid,
                        deviceCode,
                        request.ProgramVersion,
                        reason = "New viewer device registered"
                    }));

            transaction.Commit();

            return ApiResponse<ViewerLoginResponse>.Ok(
                new ViewerLoginResponse
                {
                    StoreCode = store.StoreCode,
                    DeviceCode = deviceCode,
                    LoginSuccess = true,
                    ConfigVersion = configVersion,
                    Token = token
                },
                "캠뷰어 장비가 등록되고 토큰이 발급되었습니다.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 캠뷰어 토큰 실행 인증.
    /// 
    /// 최초 로그인 이후에는 ID/비밀번호를 다시 입력하지 않고,
    /// 로컬에 저장된 토큰으로 실행 가능 여부를 확인한다.
    /// 
    /// 캠뷰어는 매장 기반 인증을 사용하므로,
    /// Viewer 토큰에는 StoreCode가 반드시 존재해야 한다.
    /// 
    /// 토큰이 유효하더라도 devices 테이블에 해당 장비가 없으면
    /// 사용 해제된 장비로 판단하고 실행을 차단한다.
    /// </summary>
    public async Task<ApiResponse<ViewerTokenVerifyResponse>> VerifyTokenAsync(
        ViewerTokenVerifyRequest request,
        string? requestIp = null)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return ApiResponse<ViewerTokenVerifyResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "토큰이 없습니다. 다시 로그인해야 합니다.");
        }

        if (string.IsNullOrWhiteSpace(request.Hwid))
        {
            return ApiResponse<ViewerTokenVerifyResponse>.Fail(
                AuthErrorCode.DuplicateHwid,
                "장비 식별값이 올바르지 않습니다.");
        }

        // 일반 API의 만료 검증은 그대로 유지하고,
        // 캠뷰어 verify-token에서만 OfflineUntil 범위의 만료 토큰을 회전 발급 후보로 허용한다.
        var validation = _tokenService.ValidateTokenForRenewal(request.Token);

        if (!validation.IsValid || validation.Payload == null)
        {
            return ApiResponse<ViewerTokenVerifyResponse>.Fail(
                validation.ErrorCode,
                validation.Message);
        }

        var payload = validation.Payload;
        var hwid = request.Hwid.Trim();
        var renewedFromExpiredToken =
            !payload.IsPermanent &&
            payload.ExpiresAt < DateTime.UtcNow;

        // 1. 캠뷰어용 토큰인지 먼저 확인한다.
        // PC캠 토큰은 StoreCode가 null일 수 있으므로,
        // StoreCode 존재 여부보다 AppType 검증이 먼저 와야 한다.
        if (payload.AppType != (int)DeviceAppType.Viewer)
        {
            await WriteAuthLogAsync(
                AuthRequestType.ViewerTokenVerify,
                payload.StoreCode,
                AuthResult.Fail,
                AuthErrorCode.InvalidLogin,
                requestIp,
                new
                {
                    Hwid = hwid,
                    payload.DeviceCode,
                    payload.AppType,
                    request.ProgramVersion,
                    reason = "Token app type is not viewer"
                });

            return ApiResponse<ViewerTokenVerifyResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "캠뷰어용 토큰이 아닙니다.");
        }

        // 2. 캠뷰어는 매장 기반 인증이므로 StoreCode가 반드시 있어야 한다.
        if (!payload.StoreCode.HasValue)
        {
            await WriteAuthLogAsync(
                AuthRequestType.ViewerTokenVerify,
                null,
                AuthResult.Fail,
                AuthErrorCode.InvalidStore,
                requestIp,
                new
                {
                    Hwid = hwid,
                    payload.DeviceCode,
                    payload.AppType,
                    request.ProgramVersion,
                    reason = "Viewer token has no store code"
                });

            return ApiResponse<ViewerTokenVerifyResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "캠뷰어 토큰에 매장 정보가 없습니다. 다시 로그인해야 합니다.");
        }

        // 이후에는 int 값으로 안전하게 사용
        var storeCode = payload.StoreCode.Value;

        // 3. HWID 일치 여부 확인
        if (!string.Equals(
                payload.Hwid,
                hwid,
                StringComparison.OrdinalIgnoreCase))
        {
            await WriteAuthLogAsync(
                AuthRequestType.ViewerTokenVerify,
                storeCode,
                AuthResult.Fail,
                AuthErrorCode.DuplicateHwid,
                requestIp,
                new
                {
                    RequestHwid = hwid,
                    TokenHwid = payload.Hwid,
                    payload.DeviceCode,
                    request.ProgramVersion,
                    reason = "HWID mismatch"
                });

            return ApiResponse<ViewerTokenVerifyResponse>.Fail(
                AuthErrorCode.DuplicateHwid,
                "현재 장비와 토큰의 장비 정보가 일치하지 않습니다.");
        }

        // 4. 장비 조회
        var device = await _deviceRepository.GetByCodeAsync(payload.DeviceCode);

        if (device == null)
        {
            await WriteAuthLogAsync(
                AuthRequestType.ViewerTokenVerify,
                storeCode,
                AuthResult.Fail,
                AuthErrorCode.DeviceNotFound,
                requestIp,
                new
                {
                    Hwid = hwid,
                    payload.DeviceCode,
                    request.ProgramVersion,
                    reason = "Device was released or deleted"
                });

            return ApiResponse<ViewerTokenVerifyResponse>.Fail(
                AuthErrorCode.DeviceNotFound,
                "사용이 해제된 장비입니다. 다시 로그인하거나 관리자에게 문의하세요.");
        }

        // 5. 장비 유형 확인
        if (device.DevAppType != (int)DeviceAppType.Viewer)
        {
            return ApiResponse<ViewerTokenVerifyResponse>.Fail(
                AuthErrorCode.DeviceNotFound,
                "캠뷰어 장비 정보가 아닙니다.");
        }

        // 6. 장비와 토큰의 정합성 확인
        // 캠뷰어 장비는 반드시 해당 매장에 연결되어 있어야 한다.
        if (device.DevStore != storeCode ||
            !string.Equals(
                device.DevHwid,
                payload.Hwid,
                StringComparison.OrdinalIgnoreCase))
        {
            await WriteAuthLogAsync(
                AuthRequestType.ViewerTokenVerify,
                storeCode,
                AuthResult.Fail,
                AuthErrorCode.DeviceNotFound,
                requestIp,
                new
                {
                    Hwid = hwid,
                    payload.DeviceCode,
                    TokenStoreCode = storeCode,
                    DeviceStoreCode = device.DevStore,
                    device.DevHwid,
                    request.ProgramVersion,
                    reason = "Device and token information mismatch"
                });

            return ApiResponse<ViewerTokenVerifyResponse>.Fail(
                AuthErrorCode.DeviceNotFound,
                "등록 장비 정보와 토큰 정보가 일치하지 않습니다.");
        }

        // 7. 매장 조회
        var store = await _storeRepository.GetByCodeAsync(storeCode);

        if (store == null)
        {
            return ApiResponse<ViewerTokenVerifyResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "매장 정보를 찾을 수 없습니다.");
        }

        if (store.StoreStatus != (int)StoreStatus.Active)
        {
            return ApiResponse<ViewerTokenVerifyResponse>.Fail(
                AuthErrorCode.StoreInactive,
                "비활성 상태의 매장입니다.");
        }

        // 8. 계약 조회
        var contract = await _contractRepository.GetByCodeAsync(payload.ContractCode);

        if (contract == null)
        {
            return ApiResponse<ViewerTokenVerifyResponse>.Fail(
                AuthErrorCode.ContractNotFound,
                "계약 정보를 찾을 수 없습니다.");
        }

        // 캠뷰어는 매장 기반 계약만 사용할 수 있으므로,
        // 토큰의 매장과 계약의 매장이 일치해야 한다.
        if (contract.ConStore != storeCode)
        {
            return ApiResponse<ViewerTokenVerifyResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "계약의 매장 정보가 토큰의 매장 정보와 일치하지 않습니다.");
        }

        // 캠뷰어 계약 수량이 없는 계약은 캠뷰어 인증에 사용할 수 없다.
        if (contract.ConView <= 0)
        {
            await WriteAuthLogAsync(
                AuthRequestType.ViewerTokenVerify,
                storeCode,
                AuthResult.Fail,
                AuthErrorCode.ContractNotFound,
                requestIp,
                new
                {
                    Hwid = hwid,
                    payload.DeviceCode,
                    contract.ConCode,
                    contract.ConView,
                    request.ProgramVersion,
                    reason = "Contract has no viewer slot"
                });

            return ApiResponse<ViewerTokenVerifyResponse>.Fail(
                AuthErrorCode.ContractNotFound,
                "사용 가능한 캠뷰어 계약 정보를 찾을 수 없습니다.");
        }

        var contractError = ValidateContract(contract);

        if (contractError != AuthErrorCode.None)
        {
            return ApiResponse<ViewerTokenVerifyResponse>.Fail(
                contractError,
                GetErrorMessage(contractError));
        }

        // 9. 설정 버전 조회
        var nvrConfig = await _nvrConfigRepository.GetByStoreAsync(storeCode);
        var configVersion = nvrConfig?.NvrVersion ?? "";

        // 10. 새 토큰 발급
        var newToken = _tokenService.CreateToken(
            storeCode: storeCode,
            contractCode: contract.ConCode,
            licenseCode: null,
            deviceCode: device.DevCode,
            appType: DeviceAppType.Viewer,
            hwid: hwid,
            contractType: (ContractType)contract.ConType,
            isPermanent: payload.IsPermanent,
            configVersion: configVersion);

        await WriteAuthLogAsync(
            AuthRequestType.ViewerTokenVerify,
            storeCode,
            AuthResult.Success,
            AuthErrorCode.None,
            requestIp,
            new
            {
                Hwid = hwid,
                device.DevCode,
                contract.ConCode,
                request.ProgramVersion,
                renewedFromExpiredToken,
                previousExpiresAt = payload.ExpiresAt,
                previousOfflineUntil = payload.OfflineUntil,
                reason = renewedFromExpiredToken
                    ? "Expired viewer token renewed after online state validation"
                    : "Viewer token verify success"
            });

        return ApiResponse<ViewerTokenVerifyResponse>.Ok(
            new ViewerTokenVerifyResponse
            {
                IsValid = true,
                StoreCode = storeCode,
                DeviceCode = device.DevCode,
                ConfigVersion = configVersion,
                Token = newToken
            },
            renewedFromExpiredToken
                ? "만료된 캠뷰어 토큰을 갱신했습니다."
                : "캠뷰어 토큰 인증이 완료되었습니다.");
    }

    /// <summary>
    /// 캠뷰어 등록 장비 목록 조회.
    /// 
    /// 슬롯 초과 시 사용자가 어떤 장비를 해제할지 선택하는 화면에서 사용할 수 있다.
    /// GetViewerDevicesWithLoginAsync로 대체 - 로그인 절차 없이 매장 코드만으로 장비 목록을 조회하는 것은 보안상 위험할 수 있다
    /// </summary>
    //public async Task<ApiResponse<List<DeviceSummaryDto>>> GetViewerDevicesAsync(int storeCode)
    //{
    //    if (storeCode <= 0)
    //    {
    //        return ApiResponse<List<DeviceSummaryDto>>.Fail(
    //            AuthErrorCode.InvalidStore,
    //            "매장 코드가 올바르지 않습니다.");
    //    }

    //    var store = await _storeRepository.GetByCodeAsync(storeCode);

    //    if (store == null)
    //    {
    //        return ApiResponse<List<DeviceSummaryDto>>.Fail(
    //            AuthErrorCode.InvalidStore,
    //            "매장 정보를 찾을 수 없습니다.");
    //    }

    //    var devices = await _deviceRepository.GetDeviceSummariesAsync(
    //        storeCode,
    //        (int)DeviceAppType.Viewer);

    //    return ApiResponse<List<DeviceSummaryDto>>.Ok(
    //        devices,
    //        "캠뷰어 등록 장비 목록을 조회했습니다.");
    //}

    /// <summary>
    /// 캠뷰어 장비 해제.
    /// 
    /// 사용자가 매장 ID/비밀번호를 입력한 뒤 특정 캠뷰어 장비를 해제한다.
    /// 해제되면 devices에서 삭제되므로,
    /// 해당 장비에 남아 있는 기존 토큰은 온라인 검증 시 더 이상 사용할 수 없다.
    /// </summary>
    public async Task<ApiResponse<ViewerDeviceReleaseResponse>> ReleaseViewerDeviceAsync(
        ViewerDeviceReleaseRequest request,
        string? requestIp = null)
    {
        if (string.IsNullOrWhiteSpace(request.StoreId))
        {
            return ApiResponse<ViewerDeviceReleaseResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "매장 ID를 입력해야 합니다.");
        }

        if (string.IsNullOrWhiteSpace(request.StorePassword))
        {
            return ApiResponse<ViewerDeviceReleaseResponse>.Fail(
                AuthErrorCode.InvalidPassword,
                "비밀번호를 입력해야 합니다.");
        }


        var storeId = request.StoreId.Trim();

        var store = await _storeRepository.GetByLoginIdAsync(storeId);

        if (store == null)
        {
            return ApiResponse<ViewerDeviceReleaseResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "매장 로그인 정보가 올바르지 않습니다.");
        }

        if (!_passwordService.VerifyStorePassword(request.StorePassword, store.StorePassword))
        {
            return ApiResponse<ViewerDeviceReleaseResponse>.Fail(
                AuthErrorCode.InvalidPassword,
                "비밀번호가 올바르지 않습니다.");
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

                return ApiResponse<ViewerDeviceReleaseResponse>.Fail(
                    AuthErrorCode.DeviceNotFound,
                    "장비 정보를 찾을 수 없습니다.");
            }

            if (device.DevStore != store.StoreCode ||
                device.DevAppType != (int)DeviceAppType.Viewer)
            {
                transaction.Rollback();

                return ApiResponse<ViewerDeviceReleaseResponse>.Fail(
                    AuthErrorCode.DeviceNotFound,
                    "해당 매장의 캠뷰어 장비가 아닙니다.");
            }

            await _deviceRepository.DeleteAsync(
                connection,
                transaction,
                request.DeviceCode);

            await _authLogRepository.InsertAsync(
                connection,
                transaction,
                CreateAuthLog(
                    AuthRequestType.ViewerDeviceRelease,
                    store.StoreCode,
                    AuthResult.Success,
                    AuthErrorCode.None,
                    requestIp,
                    new
                    {
                        request.DeviceCode,
                        device.DevHwid,
                        device.DevName,
                        ReleaseReason = request.Reason,
                        SystemReason = "Viewer device released"
                    }));

            transaction.Commit();

            return ApiResponse<ViewerDeviceReleaseResponse>.Ok(
                new ViewerDeviceReleaseResponse
                {
                    DeviceCode = request.DeviceCode,
                    Released = true
                },
                "캠뷰어 장비가 해제되었습니다.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 현재 사용 가능한 캠뷰어 활성 계약을 조회한다.
    /// 
    /// 캠뷰어 로그인에서는 PC CAM 수량이 아니라
    /// con_view 값이 1 이상인 계약만 사용 가능하다.
    /// </summary>
    private async Task<Contract?> GetValidActiveViewerContractAsync(int storeCode)
    {
        var contracts = await _contractRepository.GetActiveContractsByStoreAsync(storeCode);
        var today = DateTime.Today;

        return contracts
            .Where(c => c.Status == (int)ContractStatus.Active)
            .Where(c => c.ConView > 0)
            .Where(c => c.ConStart.Date <= today)
            .Where(c => c.ConEnd == null || c.ConEnd.Value.Date >= today)
            .OrderByDescending(c => c.ConStart)
            .ThenByDescending(c => c.ConCode)
            .FirstOrDefault();
    }

    /// <summary>
    /// 계약 상태와 기간을 검증한다.
    /// </summary>
    private static AuthErrorCode ValidateContract(Contract contract)
    {
        if (contract.Status != (int)ContractStatus.Active)
        {
            return AuthErrorCode.ContractInactive;
        }

        var today = DateTime.Today;

        if (contract.ConStart.Date > today)
        {
            return AuthErrorCode.ContractInactive;
        }

        if (contract.ConEnd != null && contract.ConEnd.Value.Date < today)
        {
            return AuthErrorCode.ContractExpired;
        }

        return AuthErrorCode.None;
    }

    /// <summary>
    /// 인증 로그 객체를 생성한다.
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
            AlError = errorCode == AuthErrorCode.None ? null : (int)errorCode,
            AlIp = requestIp,
            AlDetails = JsonSerializer.Serialize(details)
        };
    }

    /// <summary>
    /// 단독 인증 로그 저장.
    /// </summary>
    private async Task WriteAuthLogAsync(
        AuthRequestType requestType,
        int? storeCode,
        AuthResult result,
        AuthErrorCode errorCode,
        string? requestIp,
        object details)
    {
        await _authLogRepository.InsertAsync(
            CreateAuthLog(
                requestType,
                storeCode,
                result,
                errorCode,
                requestIp,
                details));
    }

    /// <summary>
    /// 오류 코드에 따른 사용자 메시지를 반환한다.
    /// </summary>
    private static string GetErrorMessage(AuthErrorCode errorCode)
    {
        return errorCode switch
        {
            AuthErrorCode.ContractInactive => "활성 상태의 계약이 아닙니다.",
            AuthErrorCode.ContractExpired => "계약 기간이 만료되었습니다.",
            AuthErrorCode.StoreInactive => "비활성 상태의 매장입니다.",
            AuthErrorCode.DeviceNotFound => "등록된 장비를 찾을 수 없습니다.",
            _ => "인증 처리 중 오류가 발생했습니다."
        };
    }

    /// <summary>
    /// 캠뷰어 등록 장비 목록을 조회한다.
    /// 
    /// 슬롯 초과 시 사용자가 어떤 장비를 해제할지 선택하는 화면에서 사용한다.
    /// storeCode만으로 조회하지 않고, 매장 ID/비밀번호를 검증한 뒤 조회한다.
    /// </summary>
    public async Task<ApiResponse<List<DeviceSummaryDto>>> GetViewerDevicesWithLoginAsync(
        ViewerDeviceListRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.StoreId))
        {
            return ApiResponse<List<DeviceSummaryDto>>.Fail(
                AuthErrorCode.InvalidLogin,
                "매장 ID를 입력해야 합니다.");
        }

        if (string.IsNullOrWhiteSpace(request.StorePassword))
        {
            return ApiResponse<List<DeviceSummaryDto>>.Fail(
                AuthErrorCode.InvalidPassword,
                "비밀번호를 입력해야 합니다.");
        }

        var storeId = request.StoreId.Trim();

        var store = await _storeRepository.GetByLoginIdAsync(storeId);

        if (store == null)
        {
            return ApiResponse<List<DeviceSummaryDto>>.Fail(
                AuthErrorCode.InvalidLogin,
                "매장 로그인 정보가 올바르지 않습니다.");
        }

        if (!_passwordService.VerifyStorePassword(request.StorePassword, store.StorePassword))
        {
            return ApiResponse<List<DeviceSummaryDto>>.Fail(
                AuthErrorCode.InvalidPassword,
                "비밀번호가 올바르지 않습니다.");
        }

        if (store.StoreStatus != (int)StoreStatus.Active)
        {
            return ApiResponse<List<DeviceSummaryDto>>.Fail(
                AuthErrorCode.StoreInactive,
                "비활성 상태의 매장입니다.");
        }

        var devices = await _deviceRepository.GetDeviceSummariesAsync(
            store.StoreCode,
            (int)DeviceAppType.Viewer);

        return ApiResponse<List<DeviceSummaryDto>>.Ok(
            devices,
            "캠뷰어 등록 장비 목록을 조회했습니다.");
    }
}
