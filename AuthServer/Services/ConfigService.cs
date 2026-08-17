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
    private const int LegacyNvrNo = 1;
    private const int MultiNvrConfigSchemaVersion = 2;

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
    /// 다중 NVR에서도 같은 매장의 모든 NVR 행은 하나의 설정 버전을 공유해야 한다.
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
        var nvrConfigs = await _nvrConfigRepository.GetListByStoreAsync(storeCode);

        if (nvrConfigs.Count == 0)
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

        string? serverVersion = GetSharedConfigVersion(nvrConfigs);

        if (serverVersion == null)
        {
            await WriteAuthLogAsync(
                AuthRequestType.ViewerConfigDownload,
                storeCode,
                AuthResult.Fail,
                AuthErrorCode.ConfigVersionConflict,
                requestIp,
                new
                {
                    request.Hwid,
                    NvrCount = nvrConfigs.Count,
                    request.ProgramVersion,
                    reason = "NVR config versions are inconsistent"
                });

            return ApiResponse<ConfigVersionResponse>.Fail(
                AuthErrorCode.ConfigVersionConflict,
                "매장 NVR 설정 버전이 서로 일치하지 않습니다.");
        }

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
                NvrCount = nvrConfigs.Count,
                request.ProgramVersion,
                reason = "Config version checked"
            });

        return ApiResponse<ConfigVersionResponse>.Ok(
            response,
            "설정 버전을 조회했습니다.");
    }

    /// <summary>
    /// 캠뷰어 최신 설정을 조회한다.
    ///
    /// 다중 NVR 매장은 ConfigSchemaVersion 2 이상인 CamViewer에만 전체 설정을 반환한다.
    /// 구버전 CamViewer에 다중 NVR 설정을 단일 NVR로 축약해서 반환하지 않는다.
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
        var nvrConfigs = await _nvrConfigRepository.GetListByStoreAsync(storeCode);

        if (nvrConfigs.Count == 0)
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
                    request.ConfigSchemaVersion,
                    request.ProgramVersion,
                    reason = "NVR config not found"
                });

            return ApiResponse<ViewerConfigResponse>.Fail(
                AuthErrorCode.NvrConfigNotFound,
                "NVR 설정을 찾을 수 없습니다.");
        }

        string? configVersion = GetSharedConfigVersion(nvrConfigs);

        if (configVersion == null)
        {
            return ApiResponse<ViewerConfigResponse>.Fail(
                AuthErrorCode.ConfigVersionConflict,
                "매장 NVR 설정 버전이 서로 일치하지 않습니다.");
        }

        bool supportsMultiNvr =
            request.ConfigSchemaVersion >= MultiNvrConfigSchemaVersion;

        if (nvrConfigs.Count > 1 && !supportsMultiNvr)
        {
            await WriteAuthLogAsync(
                AuthRequestType.ViewerConfigDownload,
                storeCode,
                AuthResult.Fail,
                AuthErrorCode.ConfigSchemaNotSupported,
                requestIp,
                new
                {
                    request.Hwid,
                    request.ConfigSchemaVersion,
                    RequiredConfigSchemaVersion = MultiNvrConfigSchemaVersion,
                    NvrCount = nvrConfigs.Count,
                    request.ProgramVersion,
                    reason = "Legacy viewer cannot load multi NVR config"
                });

            return ApiResponse<ViewerConfigResponse>.Fail(
                AuthErrorCode.ConfigSchemaNotSupported,
                "이 매장은 다중 NVR 설정을 사용합니다. 다중 NVR을 지원하는 최신 CamViewer가 필요합니다.");
        }

        var channels = await _channelConfigRepository.GetByStoreAsync(storeCode);
        var nvrDtos = nvrConfigs
            .OrderBy(x => x.NvrNo)
            .Select(ToNvrConfigDto)
            .ToList();

        var response = new ViewerConfigResponse
        {
            ConfigSchemaVersion = supportsMultiNvr
                ? MultiNvrConfigSchemaVersion
                : 1,
            StoreCode = storeCode,
            ConfigVersion = configVersion,
            Nvrs = nvrDtos,
            // 단일 NVR legacy 클라이언트 호환용이다.
            NvrConfig = nvrDtos.FirstOrDefault(),
            Channels = channels.Select(c => new ChannelConfigDto
            {
                PosNo = c.ChnPos,
                NvrNo = c.ChnNvrNo > 0 ? c.ChnNvrNo : LegacyNvrNo,
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
                ResponseConfigSchemaVersion = response.ConfigSchemaVersion,
                NvrCount = response.Nvrs.Count,
                NvrNumbers = response.Nvrs.Select(x => x.NvrNo).ToArray(),
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
    /// Schema 1:
    /// - 단일 NvrConfig를 NVR 1로 정규화한다.
    /// - 채널 NvrNo 누락을 NVR 1로 정규화한다.
    ///
    /// Schema 2:
    /// - Nvrs 전체와 채널별 NvrNo를 필수 계약으로 사용한다.
    /// - 전체 요청을 검증한 뒤 NVR/채널 목록을 하나의 트랜잭션으로 교체한다.
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
        bool isSchema2 =
            request.ConfigSchemaVersion >= MultiNvrConfigSchemaVersion;

        List<NvrConfigDto> requestedNvrs;

        if (isSchema2)
        {
            requestedNvrs = request.Nvrs ?? new List<NvrConfigDto>();

            if (requestedNvrs.Count == 0)
            {
                return ApiResponse<ConfigSyncResponse>.Fail(
                    AuthErrorCode.NvrConfigNotFound,
                    "다중 NVR 설정 목록이 없습니다.");
            }
        }
        else
        {
            if (request.NvrConfig == null)
            {
                return ApiResponse<ConfigSyncResponse>.Fail(
                    AuthErrorCode.NvrConfigNotFound,
                    "NVR 설정 정보가 없습니다.");
            }

            requestedNvrs = new List<NvrConfigDto>
            {
                CloneLegacyNvrAsNumberOne(request.NvrConfig)
            };
        }

        var normalizedNvrs = new List<NvrConfig>();
        var nvrNoSet = new HashSet<int>();

        foreach (var nvr in requestedNvrs)
        {
            if (nvr == null)
            {
                return ApiResponse<ConfigSyncResponse>.Fail(
                    AuthErrorCode.NvrConfigNotFound,
                    "NVR 설정 목록에 비어 있는 항목이 있습니다.");
            }

            int nvrNo = isSchema2 ? nvr.NvrNo : LegacyNvrNo;

            if (nvrNo <= 0)
            {
                return ApiResponse<ConfigSyncResponse>.Fail(
                    AuthErrorCode.NvrConfigNotFound,
                    "NVR 번호는 1 이상이어야 합니다.");
            }

            if (!nvrNoSet.Add(nvrNo))
            {
                return ApiResponse<ConfigSyncResponse>.Fail(
                    AuthErrorCode.NvrConfigNotFound,
                    $"NVR 번호가 중복되었습니다. NVR 번호: {nvrNo}");
            }

            var requestedProvider = nvr.NvrProvider;

            if (isSchema2 && requestedProvider == NvrProviderType.Unknown)
            {
                return ApiResponse<ConfigSyncResponse>.Fail(
                    AuthErrorCode.NvrConfigNotFound,
                    $"NVR 제조사를 지정해야 합니다. NVR 번호: {nvrNo}");
            }

            if (requestedProvider != NvrProviderType.Unknown &&
                !Enum.IsDefined(typeof(NvrProviderType), requestedProvider))
            {
                return ApiResponse<ConfigSyncResponse>.Fail(
                    AuthErrorCode.NvrConfigNotFound,
                    $"NVR 제조사 코드가 올바르지 않습니다. NVR 번호: {nvrNo}");
            }

            var provider = NormalizeProvider(requestedProvider);
            var rtspPort = isSchema2
                ? nvr.NvrRtspPort
                : NormalizeRtspPort(nvr.NvrRtspPort);

            if (string.IsNullOrWhiteSpace(nvr.NvrIp))
            {
                return ApiResponse<ConfigSyncResponse>.Fail(
                    AuthErrorCode.NvrConfigNotFound,
                    $"NVR IP를 입력해야 합니다. NVR 번호: {nvrNo}");
            }

            if (nvr.NvrPort < 1 || nvr.NvrPort > 65535)
            {
                return ApiResponse<ConfigSyncResponse>.Fail(
                    AuthErrorCode.NvrConfigNotFound,
                    $"NVR 제어/API 포트는 1부터 65535 사이여야 합니다. NVR 번호: {nvrNo}");
            }

            if (rtspPort < 1 || rtspPort > 65535)
            {
                return ApiResponse<ConfigSyncResponse>.Fail(
                    AuthErrorCode.NvrConfigNotFound,
                    $"NVR RTSP 포트는 1부터 65535 사이여야 합니다. NVR 번호: {nvrNo}");
            }

            if (string.IsNullOrWhiteSpace(nvr.NvrId))
            {
                return ApiResponse<ConfigSyncResponse>.Fail(
                    AuthErrorCode.NvrConfigNotFound,
                    $"NVR 접속 ID를 입력해야 합니다. NVR 번호: {nvrNo}");
            }

            if (string.IsNullOrWhiteSpace(nvr.NvrPassword))
            {
                return ApiResponse<ConfigSyncResponse>.Fail(
                    AuthErrorCode.NvrConfigNotFound,
                    $"NVR 접속 비밀번호를 입력해야 합니다. NVR 번호: {nvrNo}");
            }

            if (nvr.NvrChannels.GetValueOrDefault() <= 0)
            {
                return ApiResponse<ConfigSyncResponse>.Fail(
                    AuthErrorCode.NvrConfigNotFound,
                    $"NVR 채널 수는 1 이상이어야 합니다. NVR 번호: {nvrNo}");
            }

            normalizedNvrs.Add(new NvrConfig
            {
                NvrStore = storeCode,
                NvrNo = nvrNo,
                NvrProvider = provider,
                NvrId = nvr.NvrId.Trim(),
                NvrPassword = nvr.NvrPassword,
                NvrIp = nvr.NvrIp.Trim(),
                NvrPort = nvr.NvrPort,
                NvrRtspPort = rtspPort,
                NvrChannels = nvr.NvrChannels
            });
        }

        var requestedChannels = request.Channels ?? new List<ChannelConfigDto>();
        var normalizedChannels = new List<ChannelConfig>();
        var screenKeySet = new HashSet<(int PosNo, int Screen)>();
        var nvrByNo = normalizedNvrs.ToDictionary(x => x.NvrNo);

        foreach (var channel in requestedChannels)
        {
            if (channel == null)
            {
                return ApiResponse<ConfigSyncResponse>.Fail(
                    AuthErrorCode.ChannelConfigNotFound,
                    "채널 설정 목록에 비어 있는 항목이 있습니다.");
            }

            int channelNvrNo = isSchema2 ? channel.NvrNo : LegacyNvrNo;

            if (channel.PosNo <= 0)
            {
                return ApiResponse<ConfigSyncResponse>.Fail(
                    AuthErrorCode.ChannelConfigNotFound,
                    "POS 번호는 1 이상이어야 합니다.");
            }

            if (channel.Screen != 0 && channel.Screen != 1)
            {
                return ApiResponse<ConfigSyncResponse>.Fail(
                    AuthErrorCode.ChannelConfigNotFound,
                    $"화면 번호는 0 또는 1이어야 합니다. POS 번호: {channel.PosNo}");
            }

            if (!screenKeySet.Add((channel.PosNo, channel.Screen)))
            {
                return ApiResponse<ConfigSyncResponse>.Fail(
                    AuthErrorCode.ChannelConfigNotFound,
                    $"같은 POS와 화면 위치의 채널 설정이 중복되었습니다. POS 번호: {channel.PosNo}, 화면: {channel.Screen}");
            }

            if (channelNvrNo <= 0 || !nvrByNo.TryGetValue(channelNvrNo, out var referencedNvr))
            {
                return ApiResponse<ConfigSyncResponse>.Fail(
                    AuthErrorCode.ChannelConfigNotFound,
                    $"채널이 참조하는 NVR 설정을 찾을 수 없습니다. POS 번호: {channel.PosNo}, NVR 번호: {channelNvrNo}");
            }

            if (channel.ChannelNo <= 0)
            {
                return ApiResponse<ConfigSyncResponse>.Fail(
                    AuthErrorCode.ChannelConfigNotFound,
                    $"NVR 채널 번호는 1 이상이어야 합니다. POS 번호: {channel.PosNo}");
            }

            if (channel.ChannelNo > referencedNvr.NvrChannels.GetValueOrDefault())
            {
                return ApiResponse<ConfigSyncResponse>.Fail(
                    AuthErrorCode.ChannelConfigNotFound,
                    $"NVR 채널 번호가 등록된 채널 수를 초과합니다. POS 번호: {channel.PosNo}, NVR 번호: {channelNvrNo}, 채널 번호: {channel.ChannelNo}");
            }

            normalizedChannels.Add(new ChannelConfig
            {
                ChnStore = storeCode,
                ChnNvrNo = channelNvrNo,
                ChnPos = channel.PosNo,
                ChnCh = channel.ChannelNo,
                ChnScreen = channel.Screen
            });
        }

        var configVersion = string.IsNullOrWhiteSpace(request.ConfigVersion)
            ? _codeGenerateService.CreateConfigVersion()
            : request.ConfigVersion.Trim();

        foreach (var nvr in normalizedNvrs)
        {
            nvr.NvrVersion = configVersion;
        }

        using var connection = _dbContext.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            // 전체 설정 교체 순서:
            // 채널 → NVR 순으로 기존 데이터를 제거한 뒤 NVR → 채널 순으로 저장한다.
            await _channelConfigRepository.DeleteByStoreAsync(
                connection,
                transaction,
                storeCode);

            await _nvrConfigRepository.DeleteByStoreAsync(
                connection,
                transaction,
                storeCode);

            foreach (var nvr in normalizedNvrs.OrderBy(x => x.NvrNo))
            {
                await _nvrConfigRepository.UpsertAsync(
                    connection,
                    transaction,
                    nvr);
            }

            foreach (var channel in normalizedChannels
                .OrderBy(x => x.ChnPos)
                .ThenBy(x => x.ChnScreen))
            {
                await _channelConfigRepository.UpsertAsync(
                    connection,
                    transaction,
                    channel);
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
                        ConfigSchemaVersion = isSchema2
                            ? MultiNvrConfigSchemaVersion
                            : 1,
                        ConfigVersion = configVersion,
                        NvrCount = normalizedNvrs.Count,
                        NvrNumbers = normalizedNvrs.Select(x => x.NvrNo).ToArray(),
                        ChannelCount = normalizedChannels.Count,
                        request.ModifiedBy,
                        request.ProgramVersion,
                        LogReason = "Config synced"
                    }));

            transaction.Commit();

            var response = new ConfigSyncResponse
            {
                StoreCode = storeCode,
                ConfigVersion = configVersion,
                ChannelCount = normalizedChannels.Count,
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

    private static NvrConfigDto ToNvrConfigDto(NvrConfig source)
    {
        return new NvrConfigDto
        {
            NvrNo = source.NvrNo > 0 ? source.NvrNo : LegacyNvrNo,
            NvrProvider = NormalizeProvider(source.NvrProvider),
            NvrId = source.NvrId,
            NvrPassword = source.NvrPassword,
            NvrIp = source.NvrIp,
            NvrPort = source.NvrPort,
            NvrRtspPort = NormalizeRtspPort(source.NvrRtspPort),
            NvrChannels = source.NvrChannels,
            NvrVersion = source.NvrVersion ?? ""
        };
    }

    private static NvrConfigDto CloneLegacyNvrAsNumberOne(NvrConfigDto source)
    {
        return new NvrConfigDto
        {
            NvrNo = LegacyNvrNo,
            NvrProvider = source.NvrProvider,
            NvrId = source.NvrId,
            NvrPassword = source.NvrPassword,
            NvrIp = source.NvrIp,
            NvrPort = source.NvrPort,
            NvrRtspPort = source.NvrRtspPort,
            NvrChannels = source.NvrChannels,
            NvrVersion = source.NvrVersion
        };
    }

    /// <summary>
    /// 같은 매장의 모든 NVR 행이 동일한 설정 버전을 가지는지 확인한다.
    /// 일치하면 공통 버전을, 불일치하면 null을 반환한다.
    /// </summary>
    private static string? GetSharedConfigVersion(IReadOnlyCollection<NvrConfig> nvrConfigs)
    {
        var versions = nvrConfigs
            .Select(x => x.NvrVersion ?? "")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return versions.Count == 1
            ? versions[0]
            : null;
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
