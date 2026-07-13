using System.Text.Json;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.Config;
using poscam.AuthServer.Models.Dtos.Viewer;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Repositories;

namespace poscam.AuthServer.Services;

/// <summary>
/// 캠뷰어 설정 관리 서비스.
///
/// NVR 설정 조회, 설정 버전 조회, 설정 동기화를 담당한다.
/// 설정 정보는 민감한 정보가 포함되므로 토큰 검증 후 제공한다.
/// </summary>
public class ConfigService
{
    private const int DefaultRtspPort = 554;

    private readonly IDbContext _dbContext;
    private readonly StoreRepository _storeRepository;
    private readonly ContractRepository _contractRepository;
    private readonly DeviceRepository _deviceRepository;
    private readonly NvrConfigRepository _nvrConfigRepository;
    private readonly ChannelConfigRepository _channelConfigRepository;
    private readonly AuthLogRepository _authLogRepository;
    private readonly TokenService _tokenService;
    private readonly CodeGenerateService _codeGenerateService;

    public ConfigService(
        IDbContext dbContext,
        StoreRepository storeRepository,
        ContractRepository contractRepository,
        DeviceRepository deviceRepository,
        NvrConfigRepository nvrConfigRepository,
        ChannelConfigRepository channelConfigRepository,
        AuthLogRepository authLogRepository,
        TokenService tokenService,
        CodeGenerateService codeGenerateService)
    {
        _dbContext = dbContext;
        _storeRepository = storeRepository;
        _contractRepository = contractRepository;
        _deviceRepository = deviceRepository;
        _nvrConfigRepository = nvrConfigRepository;
        _channelConfigRepository = channelConfigRepository;
        _authLogRepository = authLogRepository;
        _tokenService = tokenService;
        _codeGenerateService = codeGenerateService;
    }

    /// <summary>
    /// 서버 설정 버전을 조회한다.
    /// </summary>
    public async Task<ApiResponse<ConfigVersionResponse>> GetVersionAsync(
        ConfigVersionRequest request,
        string? requestIp = null)
    {
        var tokenCheck = await ValidateViewerTokenAsync(
            request.Token,
            request.Hwid,
            requestIp,
            request.ProgramVersion,
            AuthRequestType.ViewerConfigDownload);

        if (!tokenCheck.Success)
        {
            return ApiResponse<ConfigVersionResponse>.Fail(
                tokenCheck.ErrorCode,
                tokenCheck.Message);
        }

        var storeCode = tokenCheck.Store!.StoreCode;
        var nvrConfig = await _nvrConfigRepository.GetByStoreAsync(storeCode);

        if (nvrConfig == null)
        {
            await WriteAuthLogAsync(
                AuthRequestType.ViewerConfigDownload,
                storeCode,
                AuthResult.Fail,
                AuthErrorCode.NvrConfigNotFound,
                requestIp,
                new
                {
                    request.Hwid,
                    request.ProgramVersion,
                    reason = "NVR config not found"
                });

            return ApiResponse<ConfigVersionResponse>.Fail(
                AuthErrorCode.NvrConfigNotFound,
                "NVR 설정을 찾을 수 없습니다.");
        }

        var serverVersion = nvrConfig.NvrVersion ?? "";

        var response = new ConfigVersionResponse
        {
            StoreCode = storeCode,
            ConfigVersion = serverVersion,
            IsLatest = string.Equals(
                request.LocalConfigVersion ?? "",
                serverVersion,
                StringComparison.OrdinalIgnoreCase)
        };

        await WriteAuthLogAsync(
            AuthRequestType.ViewerConfigDownload,
            storeCode,
            AuthResult.Success,
            AuthErrorCode.None,
            requestIp,
            new
            {
                request.Hwid,
                request.LocalConfigVersion,
                ServerConfigVersion = serverVersion,
                request.ProgramVersion,
                reason = "Config version checked"
            });

        return ApiResponse<ConfigVersionResponse>.Ok(
            response,
            "설정 버전을 조회했습니다.");
    }

    /// <summary>
    /// 캠뷰어 최신 설정을 조회한다.
    /// </summary>
    public async Task<ApiResponse<ViewerConfigResponse>> GetLatestConfigAsync(
        ConfigLatestRequest request,
        string? requestIp = null)
    {
        var tokenCheck = await ValidateViewerTokenAsync(
            request.Token,
            request.Hwid,
            requestIp,
            request.ProgramVersion,
            AuthRequestType.ViewerConfigDownload);

        if (!tokenCheck.Success)
        {
            return ApiResponse<ViewerConfigResponse>.Fail(
                tokenCheck.ErrorCode,
                tokenCheck.Message);
        }

        var storeCode = tokenCheck.Store!.StoreCode;
        var nvrConfig = await _nvrConfigRepository.GetByStoreAsync(storeCode);

        if (nvrConfig == null)
        {
            await WriteAuthLogAsync(
                AuthRequestType.ViewerConfigDownload,
                storeCode,
                AuthResult.Fail,
                AuthErrorCode.NvrConfigNotFound,
                requestIp,
                new
                {
                    request.Hwid,
                    request.LocalConfigVersion,
                    request.ProgramVersion,
                    reason = "NVR config not found"
                });

            return ApiResponse<ViewerConfigResponse>.Fail(
                AuthErrorCode.NvrConfigNotFound,
                "NVR 설정을 찾을 수 없습니다.");
        }

        var channels = await _channelConfigRepository.GetByStoreAsync(storeCode);
        var provider = NormalizeProvider(nvrConfig.NvrProvider);
        var rtspPort = NormalizeRtspPort(nvrConfig.NvrRtspPort);

        var response = new ViewerConfigResponse
        {
            StoreCode = storeCode,
            ConfigVersion = nvrConfig.NvrVersion ?? "",
            NvrConfig = new NvrConfigDto
            {
                NvrProvider = provider,
                NvrId = nvrConfig.NvrId,
                NvrPassword = nvrConfig.NvrPassword,
                NvrIp = nvrConfig.NvrIp,
                NvrPort = nvrConfig.NvrPort,
                NvrRtspPort = rtspPort,
                NvrChannels = nvrConfig.NvrChannels,
                NvrVersion = nvrConfig.NvrVersion ?? ""
            },
            Channels = channels.Select(c => new ChannelConfigDto
            {
                PosNo = c.ChnPos,
                ChannelNo = c.ChnCh,
                Screen = c.ChnScreen
            }).ToList()
        };

        await WriteAuthLogAsync(
            AuthRequestType.ViewerConfigDownload,
            storeCode,
            AuthResult.Success,
            AuthErrorCode.None,
            requestIp,
            new
            {
                request.Hwid,
                request.LocalConfigVersion,
                ServerConfigVersion = response.ConfigVersion,
                NvrProvider = (int)provider,
                NvrControlPort = nvrConfig.NvrPort,
                NvrRtspPort = rtspPort,
                ChannelCount = response.Channels.Count,
                request.ProgramVersion,
                reason = "Latest config downloaded"
            });

        return ApiResponse<ViewerConfigResponse>.Ok(
            response,
            "최신 설정을 조회했습니다.");
    }

    /// <summary>
    /// 캠뷰어 설정을 서버에 동기화한다.
    ///
    /// 구버전 CamViewer가 Provider/RTSP 포트를 보내지 않는 전환 기간에는
    /// Provider=Dahua, RTSP=554로 보정한다.
    /// </summary>
    public async Task<ApiResponse<ConfigSyncResponse>> SyncConfigAsync(
        ConfigSyncRequest request,
        string? requestIp = null)
    {
        var tokenCheck = await ValidateViewerTokenAsync(
            request.Token,
            request.Hwid,
            requestIp,
            request.ProgramVersion,
            AuthRequestType.ViewerConfigDownload);

        if (!tokenCheck.Success)
        {
            return ApiResponse<ConfigSyncResponse>.Fail(
                tokenCheck.ErrorCode,
                tokenCheck.Message);
        }

        var storeCode = tokenCheck.Store!.StoreCode;

        if (request.NvrConfig == null)
        {
            return ApiResponse<ConfigSyncResponse>.Fail(
                AuthErrorCode.NvrConfigNotFound,
                "NVR 설정 정보가 없습니다.");
        }

        var requestedProvider = request.NvrConfig.NvrProvider;

        if (requestedProvider != NvrProviderType.Unknown &&
            !Enum.IsDefined(typeof(NvrProviderType), requestedProvider))
        {
            return ApiResponse<ConfigSyncResponse>.Fail(
                AuthErrorCode.NvrConfigNotFound,
                "NVR 제조사 코드가 올바르지 않습니다.");
        }

        var provider = NormalizeProvider(requestedProvider);
        var rtspPort = NormalizeRtspPort(request.NvrConfig.NvrRtspPort);

        if (string.IsNullOrWhiteSpace(request.NvrConfig.NvrIp))
        {
            return ApiResponse<ConfigSyncResponse>.Fail(
                AuthErrorCode.NvrConfigNotFound,
                "NVR IP를 입력해야 합니다.");
        }

        if (request.NvrConfig.NvrPort < 1 || request.NvrConfig.NvrPort > 65535)
        {
            return ApiResponse<ConfigSyncResponse>.Fail(
                AuthErrorCode.NvrConfigNotFound,
                "NVR 제어/API 포트는 1부터 65535 사이여야 합니다.");
        }

        if (rtspPort < 1 || rtspPort > 65535)
        {
            return ApiResponse<ConfigSyncResponse>.Fail(
                AuthErrorCode.NvrConfigNotFound,
                "NVR RTSP 포트는 1부터 65535 사이여야 합니다.");
        }

        if (string.IsNullOrWhiteSpace(request.NvrConfig.NvrId))
        {
            return ApiResponse<ConfigSyncResponse>.Fail(
                AuthErrorCode.NvrConfigNotFound,
                "NVR 접속 ID를 입력해야 합니다.");
        }

        if (string.IsNullOrWhiteSpace(request.NvrConfig.NvrPassword))
        {
            return ApiResponse<ConfigSyncResponse>.Fail(
                AuthErrorCode.NvrConfigNotFound,
                "NVR 접속 비밀번호를 입력해야 합니다.");
        }

        if (request.NvrConfig.NvrChannels.GetValueOrDefault() <= 0)
        {
            return ApiResponse<ConfigSyncResponse>.Fail(
                AuthErrorCode.NvrConfigNotFound,
                "NVR 채널 수는 1 이상이어야 합니다.");
        }

        var configVersion = string.IsNullOrWhiteSpace(request.ConfigVersion)
            ? _codeGenerateService.CreateConfigVersion()
            : request.ConfigVersion.Trim();

        var channels = request.Channels ?? new List<ChannelConfigDto>();

        using var connection = _dbContext.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            var nvrConfig = new NvrConfig
            {
                NvrStore = storeCode,
                NvrProvider = provider,
                NvrId = request.NvrConfig.NvrId.Trim(),
                NvrPassword = request.NvrConfig.NvrPassword,
                NvrIp = request.NvrConfig.NvrIp.Trim(),
                NvrPort = request.NvrConfig.NvrPort,
                NvrRtspPort = rtspPort,
                NvrChannels = request.NvrConfig.NvrChannels,
                NvrVersion = configVersion
            };

            await _nvrConfigRepository.UpsertAsync(
                connection,
                transaction,
                nvrConfig);

            // 전체 동기화 방식:
            // 기존 채널 매핑을 삭제하고 새 목록을 다시 저장한다.
            await _channelConfigRepository.DeleteByStoreAsync(
                connection,
                transaction,
                storeCode);

            foreach (var channel in channels)
            {
                var config = new ChannelConfig
                {
                    ChnStore = storeCode,
                    ChnPos = channel.PosNo,
                    ChnCh = channel.ChannelNo,
                    ChnScreen = channel.Screen
                };

                await _channelConfigRepository.UpsertAsync(
                    connection,
                    transaction,
                    config);
            }

            await _authLogRepository.InsertAsync(
                connection,
                transaction,
                CreateAuthLog(
                    AuthRequestType.ViewerConfigDownload,
                    storeCode,
                    AuthResult.Success,
                    AuthErrorCode.None,
                    requestIp,
                    new
                    {
                        request.Hwid,
                        ConfigVersion = configVersion,
                        NvrProvider = (int)provider,
                        NvrControlPort = request.NvrConfig.NvrPort,
                        NvrRtspPort = rtspPort,
                        ChannelCount = channels.Count,
                        request.ModifiedBy,
                        request.ProgramVersion,
                        LogReason = "Config synced"
                    }));

            transaction.Commit();

            var response = new ConfigSyncResponse
            {
                StoreCode = storeCode,
                ConfigVersion = configVersion,
                ChannelCount = channels.Count,
                Synced = true
            };

            return ApiResponse<ConfigSyncResponse>.Ok(
                response,
                "설정이 서버에 동기화되었습니다.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 캠뷰어 토큰을 검증하고,
    /// devices 테이블에 해당 장비가 존재하는지 확인한다.
    /// </summary>
    private async Task<ViewerTokenCheckResult> ValidateViewerTokenAsync(
        string token,
        string hwid,
        string? requestIp,
        string? programVersion,
        AuthRequestType requestType)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return ViewerTokenCheckResult.Fail(
                AuthErrorCode.InvalidLogin,
                "토큰이 없습니다. 다시 로그인해야 합니다.");
        }

        if (string.IsNullOrWhiteSpace(hwid))
        {
            return ViewerTokenCheckResult.Fail(
                AuthErrorCode.DuplicateHwid,
                "장비 식별값이 올바르지 않습니다.");
        }

        var validation = _tokenService.ValidateToken(token);

        if (!validation.IsValid || validation.Payload == null)
        {
            return ViewerTokenCheckResult.Fail(
                validation.ErrorCode,
                validation.Message);
        }

        var payload = validation.Payload;
        var trimmedHwid = hwid.Trim();

        // 1. 캠뷰어용 토큰인지 먼저 확인한다.
        // PC캠 토큰은 StoreCode가 null일 수 있으므로,
        // StoreCode 검증보다 AppType 검증이 먼저 와야 한다.
        if (payload.AppType != (int)DeviceAppType.Viewer)
        {
            await WriteAuthLogAsync(
                requestType,
                payload.StoreCode,
                AuthResult.Fail,
                AuthErrorCode.InvalidLogin,
                requestIp,
                new
                {
                    Hwid = trimmedHwid,
                    payload.DeviceCode,
                    payload.AppType,
                    programVersion,
                    reason = "Token app type is not viewer"
                });

            return ViewerTokenCheckResult.Fail(
                AuthErrorCode.InvalidLogin,
                "캠뷰어용 토큰이 아닙니다.");
        }

        // 2. 캠뷰어 토큰은 매장 기반이므로 StoreCode가 반드시 필요하다.
        if (!payload.StoreCode.HasValue)
        {
            await WriteAuthLogAsync(
                requestType,
                null,
                AuthResult.Fail,
                AuthErrorCode.InvalidStore,
                requestIp,
                new
                {
                    Hwid = trimmedHwid,
                    payload.DeviceCode,
                    payload.AppType,
                    programVersion,
                    reason = "Viewer token has no store code"
                });

            return ViewerTokenCheckResult.Fail(
                AuthErrorCode.InvalidStore,
                "캠뷰어 토큰에 매장 정보가 없습니다. 다시 로그인해야 합니다.");
        }

        var storeCode = payload.StoreCode.Value;

        // 3. 토큰 HWID와 요청 HWID 일치 여부 확인
        if (!string.Equals(
                payload.Hwid,
                trimmedHwid,
                StringComparison.OrdinalIgnoreCase))
        {
            await WriteAuthLogAsync(
                requestType,
                storeCode,
                AuthResult.Fail,
                AuthErrorCode.DuplicateHwid,
                requestIp,
                new
                {
                    RequestHwid = trimmedHwid,
                    TokenHwid = payload.Hwid,
                    payload.DeviceCode,
                    programVersion,
                    reason = "HWID mismatch"
                });

            return ViewerTokenCheckResult.Fail(
                AuthErrorCode.DuplicateHwid,
                "현재 장비와 토큰의 장비 정보가 일치하지 않습니다.");
        }

        // 4. 장비 조회
        var device = await _deviceRepository.GetByCodeAsync(payload.DeviceCode);

        if (device == null)
        {
            await WriteAuthLogAsync(
                requestType,
                storeCode,
                AuthResult.Fail,
                AuthErrorCode.DeviceNotFound,
                requestIp,
                new
                {
                    Hwid = trimmedHwid,
                    payload.DeviceCode,
                    programVersion,
                    reason = "Device was released or deleted"
                });

            return ViewerTokenCheckResult.Fail(
                AuthErrorCode.DeviceNotFound,
                "사용이 해제된 장비입니다. 다시 로그인하거나 관리자에게 문의하세요.");
        }

        // 5. 장비 정보와 토큰 정보 정합성 검증
        if (device.DevAppType != (int)DeviceAppType.Viewer ||
            device.DevStore != storeCode ||
            !string.Equals(
                device.DevHwid,
                payload.Hwid,
                StringComparison.OrdinalIgnoreCase))
        {
            await WriteAuthLogAsync(
                requestType,
                storeCode,
                AuthResult.Fail,
                AuthErrorCode.DeviceNotFound,
                requestIp,
                new
                {
                    Hwid = trimmedHwid,
                    payload.DeviceCode,
                    TokenStoreCode = storeCode,
                    DeviceStoreCode = device.DevStore,
                    DeviceAppType = device.DevAppType,
                    payload.AppType,
                    programVersion,
                    reason = "Device and token information mismatch"
                });

            return ViewerTokenCheckResult.Fail(
                AuthErrorCode.DeviceNotFound,
                "등록 장비 정보와 토큰 정보가 일치하지 않습니다.");
        }

        // 6. 매장 조회
        var store = await _storeRepository.GetByCodeAsync(storeCode);

        if (store == null)
        {
            return ViewerTokenCheckResult.Fail(
                AuthErrorCode.InvalidStore,
                "매장 정보를 찾을 수 없습니다.");
        }

        if (store.StoreStatus != (int)StoreStatus.Active)
        {
            return ViewerTokenCheckResult.Fail(
                AuthErrorCode.StoreInactive,
                "비활성 상태의 매장입니다.");
        }

        // 7. 계약 조회
        var contract = await _contractRepository.GetByCodeAsync(payload.ContractCode);

        if (contract == null)
        {
            return ViewerTokenCheckResult.Fail(
                AuthErrorCode.ContractNotFound,
                "계약 정보를 찾을 수 없습니다.");
        }

        if (contract.ConStore != storeCode)
        {
            return ViewerTokenCheckResult.Fail(
                AuthErrorCode.InvalidStore,
                "계약의 매장 정보가 토큰의 매장 정보와 일치하지 않습니다.");
        }

        // 8. 계약 상태와 기간 검증
        var contractError = ValidateContract(contract);

        if (contractError != AuthErrorCode.None)
        {
            return ViewerTokenCheckResult.Fail(
                contractError,
                GetErrorMessage(contractError));
        }

        return ViewerTokenCheckResult.Ok(payload, device, store, contract);
    }

    private static NvrProviderType NormalizeProvider(NvrProviderType provider)
    {
        return provider == NvrProviderType.Unknown
            ? NvrProviderType.Dahua
            : provider;
    }

    private static int NormalizeRtspPort(int rtspPort)
    {
        return rtspPort <= 0 ? DefaultRtspPort : rtspPort;
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
            AlError = errorCode == AuthErrorCode.None
                ? null
                : (int)errorCode,
            AlIp = requestIp,
            AlDetails = JsonSerializer.Serialize(details)
        };
    }

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

    private class ViewerTokenCheckResult
    {
        public bool Success { get; set; }

        public AuthErrorCode ErrorCode { get; set; }

        public string Message { get; set; } = "";

        public AuthTokenPayloadDto? Payload { get; set; }

        public Device? Device { get; set; }

        public Store? Store { get; set; }

        public Contract? Contract { get; set; }

        public static ViewerTokenCheckResult Ok(
            AuthTokenPayloadDto payload,
            Device device,
            Store store,
            Contract contract)
        {
            return new ViewerTokenCheckResult
            {
                Success = true,
                ErrorCode = AuthErrorCode.None,
                Message = "토큰이 유효합니다.",
                Payload = payload,
                Device = device,
                Store = store,
                Contract = contract
            };
        }

        public static ViewerTokenCheckResult Fail(
            AuthErrorCode errorCode,
            string message)
        {
            return new ViewerTokenCheckResult
            {
                Success = false,
                ErrorCode = errorCode,
                Message = message
            };
        }
    }
}
