using System.Text.Json;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.Pccam;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Repositories;

namespace poscam.AuthServer.Services;

/// <summary>
/// PC캠 인증 서비스.
/// 
/// PC캠 최초 인증, 실행 인증, 하트비트를 담당한다.
/// PC캠은 인증키 1개가 특정 HWID 1대에 귀속되는 구조다.
/// 
/// 인증 흐름:
/// 1. 최초 인증: LicenseKey + HWID
/// 2. 실행 인증: Token + HWID
/// 3. 하트비트: HWID
/// </summary>
public class PccamAuthService
{
    private readonly IDbContext _dbContext;
    private readonly StoreRepository _storeRepository;
    private readonly ContractRepository _contractRepository;
    private readonly LicenseKeyRepository _licenseKeyRepository;
    private readonly DeviceRepository _deviceRepository;
    private readonly AuthLogRepository _authLogRepository;
    private readonly LicenseLogRepository _licenseLogRepository;
    private readonly LicenseKeyService _licenseKeyService;
    private readonly TokenService _tokenService;
    private readonly CodeGenerateService _codeGenerateService;

    public PccamAuthService(
        IDbContext dbContext,
        StoreRepository storeRepository,
        ContractRepository contractRepository,
        LicenseKeyRepository licenseKeyRepository,
        DeviceRepository deviceRepository,
        AuthLogRepository authLogRepository,
        LicenseLogRepository licenseLogRepository,
        LicenseKeyService licenseKeyService,
        TokenService tokenService,
        CodeGenerateService codeGenerateService)
    {
        _dbContext = dbContext;
        _storeRepository = storeRepository;
        _contractRepository = contractRepository;
        _licenseKeyRepository = licenseKeyRepository;
        _deviceRepository = deviceRepository;
        _authLogRepository = authLogRepository;
        _licenseLogRepository = licenseLogRepository;
        _licenseKeyService = licenseKeyService;
        _tokenService = tokenService;
        _codeGenerateService = codeGenerateService;
    }

    /// <summary>
    /// PC캠 최초 인증.
    /// 
    /// 인증키와 HWID를 기준으로 라이선스 사용 가능 여부를 확인하고,
    /// 승인되면 현재 장비를 등록한 뒤 정식 인증 토큰을 발급한다.
    /// 
    /// 계약에 매장이 연결되어 있을 수도 있고,
    /// 매장 없이 파트너사 기준으로만 생성된 계약일 수도 있다.
    /// 
    /// POS 번호는 인증 단계에서 사용하지 않으며 최초 등록 시 0으로 저장한다.
    /// </summary>
    public async Task<ApiResponse<PccamActivateResponse>> ActivateAsync(
    PccamActivateRequest request,
    string? requestIp = null)
    {
        int? unknownStoreCode = null;

        var normalizedLicenseKey =
            _licenseKeyService.NormalizeLicenseKey(request.LicenseKey);

        if (!_licenseKeyService.IsValidPccamLicenseKey(normalizedLicenseKey))
        {
            await WriteAuthLogAsync(
                AuthRequestType.PccamActivate,
                unknownStoreCode,
                AuthResult.Fail,
                AuthErrorCode.InvalidLicenseFormat,
                requestIp,
                new
                {
                    request.Hwid,
                    reason = "Invalid license key format"
                });

            return ApiResponse<PccamActivateResponse>.Fail(
                AuthErrorCode.InvalidLicenseFormat,
                "인증키 형식이 올바르지 않습니다.");
        }

        if (string.IsNullOrWhiteSpace(request.Hwid))
        {
            return ApiResponse<PccamActivateResponse>.Fail(
                AuthErrorCode.DuplicateHwid,
                "장비 식별값이 올바르지 않습니다.");
        }

        var hwid = request.Hwid.Trim();

        using var connection = _dbContext.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            var license = await _licenseKeyRepository.GetByKeyAsync(
                connection,
                transaction,
                normalizedLicenseKey);

            if (license == null)
            {
                await _authLogRepository.InsertAsync(
                    connection,
                    transaction,
                    CreateAuthLog(
                        AuthRequestType.PccamActivate,
                        unknownStoreCode,
                        AuthResult.Fail,
                        AuthErrorCode.LicenseNotFound,
                        requestIp,
                        new
                        {
                            Hwid = hwid,
                            licenseKey = normalizedLicenseKey,
                            reason = "License not found"
                        }));

                transaction.Commit();

                return ApiResponse<PccamActivateResponse>.Fail(
                    AuthErrorCode.LicenseNotFound,
                    "인증키를 찾을 수 없습니다.");
            }

            var contract = await _contractRepository.GetByCodeAsync(
                connection,
                transaction,
                license.LicContract);

            if (contract == null)
            {
                await _authLogRepository.InsertAsync(
                    connection,
                    transaction,
                    CreateAuthLog(
                        AuthRequestType.PccamActivate,
                        unknownStoreCode,
                        AuthResult.Fail,
                        AuthErrorCode.ContractNotFound,
                        requestIp,
                        new
                        {
                            Hwid = hwid,
                            license.LicCode,
                            reason = "Contract not found"
                        }));

                transaction.Commit();

                return ApiResponse<PccamActivateResponse>.Fail(
                    AuthErrorCode.ContractNotFound,
                    "계약 정보를 찾을 수 없습니다.");
            }

            // 계약에 매장이 연결되어 있을 수도 있고,
            // 매장 없이 파트너사 기준으로만 생성된 계약일 수도 있다.
            var storeCode = contract.ConStore;

            Store? store = null;

            // 매장과 연결된 계약인 경우에만 매장을 조회한다.
            if (storeCode.HasValue)
            {
                store = await _storeRepository.GetByCodeAsync(
                    connection,
                    transaction,
                    storeCode.Value);

                if (store == null)
                {
                    await _authLogRepository.InsertAsync(
                        connection,
                        transaction,
                        CreateAuthLog(
                            AuthRequestType.PccamActivate,
                            storeCode,
                            AuthResult.Fail,
                            AuthErrorCode.InvalidStore,
                            requestIp,
                            new
                            {
                                Hwid = hwid,
                                license.LicCode,
                                contract.ConCode,
                                StoreCode = storeCode,
                                reason = "Store not found"
                            }));

                    transaction.Commit();

                    return ApiResponse<PccamActivateResponse>.Fail(
                        AuthErrorCode.InvalidStore,
                        "계약에 연결된 매장 정보를 찾을 수 없습니다.");
                }
            }

            // 매장 유무와 관계없이 계약 상태, 계약 기간, 라이선스 상태는 검증한다.
            // 단, Store가 null이면 매장 상태 검증만 제외된다.
            var validationError = ValidateStoreContractAndLicense(
                store,
                contract,
                license,
                allowReadyOrResetLicense: true);

            if (validationError != AuthErrorCode.None)
            {
                await _authLogRepository.InsertAsync(
                    connection,
                    transaction,
                    CreateAuthLog(
                        AuthRequestType.PccamActivate,
                        storeCode,
                        AuthResult.Fail,
                        validationError,
                        requestIp,
                        new
                        {
                            Hwid = hwid,
                            license.LicCode,
                            contract.ConCode,
                            StoreCode = storeCode,
                            reason = validationError.ToString()
                        }));

                transaction.Commit();

                return ApiResponse<PccamActivateResponse>.Fail(
                    validationError,
                    GetErrorMessage(validationError));
            }

            var existingDeviceByLicense =
                await _deviceRepository.FindByLicenseAsync(
                    connection,
                    transaction,
                    license.LicCode);

            if (existingDeviceByLicense != null)
            {
                var isSameDevice =
                    string.Equals(
                        existingDeviceByLicense.DevHwid,
                        hwid,
                        StringComparison.OrdinalIgnoreCase)
                    && existingDeviceByLicense.DevStore == storeCode
                    && existingDeviceByLicense.DevAppType == (int)DeviceAppType.Pccam;

                if (isSameDevice)
                {
                    var tokenForExisting = _tokenService.CreateToken(
                        storeCode: storeCode,
                        contractCode: contract.ConCode,
                        licenseCode: license.LicCode,
                        deviceCode: existingDeviceByLicense.DevCode,
                        appType: DeviceAppType.Pccam,
                        hwid: hwid,
                        contractType: (ContractType)contract.ConType,
                        isPermanent: false);

                    await _authLogRepository.InsertAsync(
                        connection,
                        transaction,
                        CreateAuthLog(
                            AuthRequestType.PccamActivate,
                            storeCode,
                            AuthResult.Success,
                            AuthErrorCode.None,
                            requestIp,
                            new
                            {
                                Hwid = hwid,
                                license.LicCode,
                                existingDeviceByLicense.DevCode,
                                StoreCode = storeCode,
                                reason = "Already activated same device"
                            }));

                    transaction.Commit();

                    return ApiResponse<PccamActivateResponse>.Ok(
                        new PccamActivateResponse
                        {
                            DeviceCode = existingDeviceByLicense.DevCode,
                            StoreCode = storeCode,
                            LicenseCode = license.LicCode,
                            Activated = true,
                            Token = tokenForExisting
                        },
                        "이미 등록된 장비입니다. 인증을 갱신했습니다.");
                }

                await _authLogRepository.InsertAsync(
                    connection,
                    transaction,
                    CreateAuthLog(
                        AuthRequestType.PccamActivate,
                        storeCode,
                        AuthResult.Fail,
                        AuthErrorCode.LicenseAlreadyUsed,
                        requestIp,
                        new
                        {
                            Hwid = hwid,
                            license.LicCode,
                            existingDeviceByLicense.DevCode,
                            existingDeviceByLicense.DevHwid,
                            ExistingStoreCode = existingDeviceByLicense.DevStore,
                            CurrentStoreCode = storeCode,
                            reason = "License already used by another device"
                        }));

                transaction.Commit();

                return ApiResponse<PccamActivateResponse>.Fail(
                    AuthErrorCode.LicenseAlreadyUsed,
                    "이미 다른 PC에 등록된 인증키입니다. PC 교체 또는 재설치가 필요한 경우 관리자에게 문의하세요.");
            }

            var device = new Device
            {
                // 매장 연결 계약이면 매장코드 저장,
                // 매장 없는 계약이면 null 저장
                DevStore = storeCode,

                DevLicense = license.LicCode,
                DevAppType = (int)DeviceAppType.Pccam,
                DevHwid = hwid,

                // POS 번호는 인증 시점에 설정하지 않는다.
                // 이후 NVR/뷰어 설정 단계에서 별도로 연결한다.
                DevPos = 0,

                DevName = string.IsNullOrWhiteSpace(request.DeviceName)
                    ? "PC캠 장비"
                    : request.DeviceName.Trim()
            };

            var deviceCode = await _deviceRepository.InsertAsync(
                connection,
                transaction,
                device);

            await _licenseKeyRepository.UpdateStatusAsync(
                connection,
                transaction,
                license.LicCode,
                (int)LicenseStatus.Activated);

            await _licenseLogRepository.InsertAsync(
                connection,
                transaction,
                new LicenseLog
                {
                    LigCode = _codeGenerateService.CreateLicenseLogCode(),
                    LigLicense = license.LicCode,

                    // 매장 연결 계약이면 매장코드 저장,
                    // 매장 없는 계약이면 null 저장
                    LigStore = storeCode,

                    LigHwid = hwid,
                    LigActionType = (int)LicenseActionType.Activate,
                    LigReason = "PC캠 최초 인증"
                });

            var token = _tokenService.CreateToken(
                storeCode: storeCode,
                contractCode: contract.ConCode,
                licenseCode: license.LicCode,
                deviceCode: deviceCode,
                appType: DeviceAppType.Pccam,
                hwid: hwid,
                contractType: (ContractType)contract.ConType,
                isPermanent: false);

            await _authLogRepository.InsertAsync(
                connection,
                transaction,
                CreateAuthLog(
                    AuthRequestType.PccamActivate,
                    storeCode,
                    AuthResult.Success,
                    AuthErrorCode.None,
                    requestIp,
                    new
                    {
                        Hwid = hwid,
                        deviceCode,
                        license.LicCode,
                        contract.ConCode,
                        StoreCode = storeCode
                    }));

            transaction.Commit();

            return ApiResponse<PccamActivateResponse>.Ok(
                new PccamActivateResponse
                {
                    DeviceCode = deviceCode,
                    StoreCode = storeCode,
                    LicenseCode = license.LicCode,
                    Activated = true,
                    Token = token
                },
                "PC캠 인증이 완료되었습니다.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// PC캠 실행 인증.
    /// 
    /// 최초 인증 이후에는 인증키가 아니라 토큰을 사용한다.
    /// 서버는 토큰, HWID, 장비 등록상태, 계약/라이선스 상태를 검증하고
    /// 정상인 경우 새 토큰을 발급한다.
    /// 
    /// 정책:
    /// - 매장 연결 계약은 매장 상태까지 검증한다.
    /// - 매장 없는 계약은 매장 상태 검증만 제외하고,
    ///   계약 상태 / 계약 기간 / 라이선스 상태는 동일하게 검증한다.
    /// - 토큰의 StoreCode, 장비의 DevStore, 계약의 ConStore는 서로 일치해야 한다.
    ///   단, 매장 없는 계약은 모두 null이어야 정상이다.
    /// </summary>
    public async Task<ApiResponse<PccamVerifyResponse>> VerifyAsync(
        PccamVerifyRequest request,
        string? requestIp = null)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return ApiResponse<PccamVerifyResponse>.Fail(
                AuthErrorCode.TokenInvalid,
                "토큰이 없습니다. 인증 상태를 다시 확인해야 합니다.");
        }

        if (string.IsNullOrWhiteSpace(request.Hwid))
        {
            return ApiResponse<PccamVerifyResponse>.Fail(
                AuthErrorCode.DuplicateHwid,
                "장비 식별값이 올바르지 않습니다.");
        }

        var validation = _tokenService.ValidateToken(request.Token);

        if (!validation.IsValid || validation.Payload == null)
        {
            return ApiResponse<PccamVerifyResponse>.Fail(
                validation.ErrorCode,
                validation.Message);
        }

        var payload = validation.Payload;
        var hwid = request.Hwid.Trim();

        // PC캠용 토큰인지 확인
        if (payload.AppType != (int)DeviceAppType.Pccam)
        {
            await WriteAuthLogAsync(
                AuthRequestType.PccamVerify,
                payload.StoreCode,
                AuthResult.Fail,
                AuthErrorCode.TokenInvalid,
                requestIp,
                new
                {
                    Hwid = hwid,
                    payload.DeviceCode,
                    payload.AppType,
                    reason = "Token app type is not PC cam"
                });

            return ApiResponse<PccamVerifyResponse>.Fail(
                AuthErrorCode.TokenInvalid,
                "PC캠용 토큰이 아닙니다.");
        }

        // 요청 HWID와 토큰 HWID 일치 여부 확인
        if (!string.Equals(
                payload.Hwid,
                hwid,
                StringComparison.OrdinalIgnoreCase))
        {
            await WriteAuthLogAsync(
                AuthRequestType.PccamVerify,
                payload.StoreCode,
                AuthResult.Fail,
                AuthErrorCode.HwidMismatch,
                requestIp,
                new
                {
                    RequestHwid = hwid,
                    TokenHwid = payload.Hwid,
                    payload.DeviceCode,
                    reason = "HWID mismatch"
                });

            return ApiResponse<PccamVerifyResponse>.Fail(
                AuthErrorCode.HwidMismatch,
                "현재 장비와 토큰의 장비 정보가 일치하지 않습니다.");
        }

        // PC캠 토큰에는 라이선스 코드가 반드시 있어야 한다.
        if (payload.LicenseCode == null)
        {
            await WriteAuthLogAsync(
                AuthRequestType.PccamVerify,
                payload.StoreCode,
                AuthResult.Fail,
                AuthErrorCode.LicenseNotFound,
                requestIp,
                new
                {
                    Hwid = hwid,
                    payload.DeviceCode,
                    reason = "PC cam token has no license code"
                });

            return ApiResponse<PccamVerifyResponse>.Fail(
                AuthErrorCode.LicenseNotFound,
                "토큰에 연결된 라이선스 정보가 없습니다.");
        }

        // 장비 조회
        var device = await _deviceRepository.GetByCodeAsync(
            payload.DeviceCode);

        if (device == null)
        {
            await WriteAuthLogAsync(
                AuthRequestType.PccamVerify,
                payload.StoreCode,
                AuthResult.Fail,
                AuthErrorCode.DeviceNotFound,
                requestIp,
                new
                {
                    Hwid = hwid,
                    payload.DeviceCode,
                    reason = "Device was released or deleted"
                });

            return ApiResponse<PccamVerifyResponse>.Fail(
                AuthErrorCode.DeviceNotFound,
                "사용이 해제된 장비입니다. 인증 상태를 다시 확인하세요.");
        }

        // PC캠 장비인지 확인
        if (device.DevAppType != (int)DeviceAppType.Pccam)
        {
            return ApiResponse<PccamVerifyResponse>.Fail(
                AuthErrorCode.DeviceNotFound,
                "PC캠 장비 정보가 아닙니다.");
        }

        // 토큰과 실제 장비 정보 정합성 검증
        // 매장 없는 계약이면 DevStore와 StoreCode가 모두 null이어야 정상이다.
        var isDeviceMismatch =
            device.DevStore != payload.StoreCode
            || device.DevLicense != payload.LicenseCode
            || !string.Equals(
                device.DevHwid,
                payload.Hwid,
                StringComparison.OrdinalIgnoreCase);

        if (isDeviceMismatch)
        {
            await WriteAuthLogAsync(
                AuthRequestType.PccamVerify,
                payload.StoreCode,
                AuthResult.Fail,
                AuthErrorCode.DeviceNotFound,
                requestIp,
                new
                {
                    Hwid = hwid,
                    payload.DeviceCode,
                    TokenStoreCode = payload.StoreCode,
                    DeviceStoreCode = device.DevStore,
                    payload.LicenseCode,
                    device.DevLicense,
                    TokenHwid = payload.Hwid,
                    DeviceHwid = device.DevHwid,
                    reason = "Device and token information mismatch"
                });

            return ApiResponse<PccamVerifyResponse>.Fail(
                AuthErrorCode.DeviceNotFound,
                "등록 장비 정보와 토큰 정보가 일치하지 않습니다.");
        }

        // 라이선스 조회
        var license = await _licenseKeyRepository.GetByCodeAsync(
            payload.LicenseCode.Value);

        if (license == null)
        {
            return ApiResponse<PccamVerifyResponse>.Fail(
                AuthErrorCode.LicenseNotFound,
                "라이선스 정보를 찾을 수 없습니다.");
        }

        // 계약 조회
        var contract = await _contractRepository.GetByCodeAsync(
            payload.ContractCode);

        if (contract == null)
        {
            return ApiResponse<PccamVerifyResponse>.Fail(
                AuthErrorCode.ContractNotFound,
                "계약 정보를 찾을 수 없습니다.");
        }

        // 라이선스-계약 관계와
        // 토큰 StoreCode-계약 ConStore 관계를 함께 검증한다.
        //
        // 매장 연결 계약:
        // - payload.StoreCode = contract.ConStore
        //
        // 매장 없는 계약:
        // - payload.StoreCode = null
        // - contract.ConStore = null
        var isLicenseContractMismatch =
            license.LicContract != contract.ConCode
            || contract.ConStore != payload.StoreCode;

        if (isLicenseContractMismatch)
        {
            await WriteAuthLogAsync(
                AuthRequestType.PccamVerify,
                payload.StoreCode,
                AuthResult.Fail,
                AuthErrorCode.LicenseContractMismatch,
                requestIp,
                new
                {
                    Hwid = hwid,
                    payload.DeviceCode,
                    payload.LicenseCode,
                    payload.ContractCode,
                    TokenStoreCode = payload.StoreCode,
                    ActualLicenseContract = license.LicContract,
                    ActualContractCode = contract.ConCode,
                    ActualContractStore = contract.ConStore,
                    reason = "License, contract, store relation mismatch"
                });

            return ApiResponse<PccamVerifyResponse>.Fail(
                AuthErrorCode.LicenseContractMismatch,
                "라이선스와 계약정보가 일치하지 않습니다.");
        }

        // 계약 기준의 실제 매장코드
        // 매장 없는 계약이면 null
        var storeCode = contract.ConStore;

        Store? store = null;

        // 매장 연결 계약인 경우에만 매장 조회
        if (storeCode.HasValue)
        {
            store = await _storeRepository.GetByCodeAsync(
                storeCode.Value);

            if (store == null)
            {
                await WriteAuthLogAsync(
                    AuthRequestType.PccamVerify,
                    storeCode,
                    AuthResult.Fail,
                    AuthErrorCode.InvalidStore,
                    requestIp,
                    new
                    {
                        Hwid = hwid,
                        payload.DeviceCode,
                        license.LicCode,
                        contract.ConCode,
                        StoreCode = storeCode,
                        reason = "Store not found"
                    });

                return ApiResponse<PccamVerifyResponse>.Fail(
                    AuthErrorCode.InvalidStore,
                    "계약에 연결된 매장 정보를 찾을 수 없습니다.");
            }
        }

        // 매장이 없는 계약도 계약/라이선스 상태 검증은 반드시 수행한다.
        // Store가 null이면 매장 상태 검증만 제외된다.
        var validationError = ValidateStoreContractAndLicense(
            store,
            contract,
            license,
            allowReadyOrResetLicense: false);

        if (validationError != AuthErrorCode.None)
        {
            await WriteAuthLogAsync(
                AuthRequestType.PccamVerify,
                storeCode,
                AuthResult.Fail,
                validationError,
                requestIp,
                new
                {
                    Hwid = hwid,
                    payload.DeviceCode,
                    license.LicCode,
                    contract.ConCode,
                    StoreCode = storeCode,
                    reason = validationError.ToString()
                });

            return ApiResponse<PccamVerifyResponse>.Fail(
                validationError,
                GetErrorMessage(validationError));
        }

        // 정상 인증이면 새 토큰 발급
        var newToken = _tokenService.CreateToken(
            storeCode: storeCode,
            contractCode: contract.ConCode,
            licenseCode: license.LicCode,
            deviceCode: device.DevCode,
            appType: DeviceAppType.Pccam,
            hwid: hwid,
            contractType: (ContractType)contract.ConType,
            isPermanent: payload.IsPermanent);

        await WriteAuthLogAsync(
            AuthRequestType.PccamVerify,
            storeCode,
            AuthResult.Success,
            AuthErrorCode.None,
            requestIp,
            new
            {
                Hwid = hwid,
                device.DevCode,
                license.LicCode,
                contract.ConCode,
                StoreCode = storeCode,
                reason = "PC cam token verify success"
            });

        return ApiResponse<PccamVerifyResponse>.Ok(
            new PccamVerifyResponse
            {
                IsValid = true,
                StoreCode = storeCode,
                DeviceCode = device.DevCode,
                Token = newToken
            },
            "PC캠 인증이 유효합니다.");
    }

    /// <summary>
    /// PC캠 하트비트.
    /// 
    /// 현재는 인증 판단용이 아니라
    /// 서버 auth_logs에 장비 생존 기록을 남기는 보조 API로 사용한다.
    /// 
    /// 매장 없는 계약으로 등록된 장비는
    /// DevStore가 null일 수 있으며,
    /// 이 경우 인증 로그의 매장코드도 null로 기록한다.
    /// </summary>
    public async Task<ApiResponse<PccamHeartbeatResponse>> HeartbeatAsync(
        PccamHeartbeatRequest request,
        string? requestIp = null)
    {
        int? unknownStoreCode = null;

        if (string.IsNullOrWhiteSpace(request.Hwid))
        {
            return ApiResponse<PccamHeartbeatResponse>.Fail(
                AuthErrorCode.DuplicateHwid,
                "장비 식별값이 올바르지 않습니다.");
        }

        var hwid = request.Hwid.Trim();

        var device = await _deviceRepository.FindPccamDeviceAsync(hwid);

        if (device == null)
        {
            await WriteAuthLogAsync(
                AuthRequestType.PccamHeartbeat,
                unknownStoreCode,
                AuthResult.Fail,
                AuthErrorCode.DeviceNotFound,
                requestIp,
                new
                {
                    Hwid = hwid,
                    reason = "Heartbeat device not found"
                });

            return ApiResponse<PccamHeartbeatResponse>.Fail(
                AuthErrorCode.DeviceNotFound,
                "등록된 PC캠 장비를 찾을 수 없습니다.");
        }

        await WriteAuthLogAsync(
            AuthRequestType.PccamHeartbeat,
            device.DevStore,
            AuthResult.Success,
            AuthErrorCode.None,
            requestIp,
            new
            {
                Hwid = hwid,
                device.DevCode,
                StoreCode = device.DevStore
            });

        return ApiResponse<PccamHeartbeatResponse>.Ok(
            new PccamHeartbeatResponse
            {
                IsValid = true,
                ServerTime = DateTime.UtcNow
            },
            "PC캠 하트비트가 기록되었습니다.");
    }

    /// <summary>
    /// 매장, 계약, 라이선스 상태를 공통 검증한다.
    /// 
    /// 매장이 연결된 계약:
    /// - 매장 상태 검증
    /// - 계약 상태/기간 검증
    /// - 라이선스 상태 검증
    /// 
    /// 매장이 없는 계약:
    /// - 매장 상태는 검증하지 않음
    /// - 계약 상태/기간은 반드시 검증
    /// - 라이선스 상태도 반드시 검증
    /// </summary>
    private static AuthErrorCode ValidateStoreContractAndLicense(
        Store? store,
        Contract contract,
        LicenseKey license,
        bool allowReadyOrResetLicense)
    {
        // 1. 매장이 연결된 계약인 경우에만 매장 상태 검증
        if (store != null &&
            store.StoreStatus != (int)StoreStatus.Active)
        {
            return AuthErrorCode.StoreInactive;
        }

        // 2. 계약 상태는 매장 유무와 관계없이 반드시 검증
        if (contract.Status != (int)ContractStatus.Active)
        {
            return AuthErrorCode.ContractInactive;
        }

        // 3. 계약 기간도 매장 유무와 관계없이 반드시 검증
        var today = DateTime.Today;

        if (contract.ConStart.Date > today)
        {
            return AuthErrorCode.ContractInactive;
        }

        if (contract.ConEnd != null &&
            contract.ConEnd.Value.Date < today)
        {
            return AuthErrorCode.ContractExpired;
        }

        // 4. 폐기된 라이선스는 무조건 차단
        if (license.LicStatus == (int)LicenseStatus.Revoked)
        {
            return AuthErrorCode.LicenseRevoked;
        }

        // 5. 최초 인증 시 허용 가능한 라이선스 상태 검증
        if (allowReadyOrResetLicense)
        {
            if (license.LicStatus == (int)LicenseStatus.Ready ||
                license.LicStatus == (int)LicenseStatus.Reset ||
                license.LicStatus == (int)LicenseStatus.Activated)
            {
                return AuthErrorCode.None;
            }

            return AuthErrorCode.LicenseNotFound;
        }

        // 6. 실행 인증 시에는 이미 활성화된 라이선스만 허용
        if (license.LicStatus != (int)LicenseStatus.Activated)
        {
            return AuthErrorCode.LicenseNotFound;
        }

        return AuthErrorCode.None;
    }

    /// <summary>
    /// 인증 로그를 생성한다.
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

    /// <summary>
    /// 단독 인증 로그 저장.
    /// 트랜잭션이 필요 없는 실행 인증/하트비트에서 사용한다.
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
            AuthErrorCode.StoreInactive => "비활성 상태의 매장입니다.",
            AuthErrorCode.ContractInactive => "활성 상태의 계약이 아닙니다.",
            AuthErrorCode.ContractExpired => "계약 기간이 만료되었습니다.",
            AuthErrorCode.LicenseRevoked => "폐기된 라이선스입니다.",
            AuthErrorCode.LicenseAlreadyUsed => "이미 사용 중인 라이선스입니다.",
            AuthErrorCode.LicenseNotFound => "유효한 라이선스가 아닙니다.",
            AuthErrorCode.LicenseContractMismatch => "라이선스와 계약정보가 일치하지 않습니다.",
            _ => "인증 처리 중 오류가 발생했습니다."
        };
    }
}