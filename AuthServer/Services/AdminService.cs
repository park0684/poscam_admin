using poscam.AuthServer.Models.Dtos.Admin;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Repositories;

namespace poscam.AuthServer.Services;

/// <summary>
/// 관리자 기능 서비스.
/// 
/// 매장 등록, 계약 등록, PC 캠 라이선스 발급,
/// NVR 설정 저장, 채널 매핑 저장을 담당한다.
/// </summary>
public class AdminService
{
    private readonly IDbContext _dbContext;
    private readonly StoreRepository _storeRepository;
    private readonly ContractRepository _contractRepository;
    private readonly LicenseKeyRepository _licenseKeyRepository;
    private readonly LicenseLogRepository _licenseLogRepository;
    private readonly NvrConfigRepository _nvrConfigRepository;
    private readonly ChannelConfigRepository _channelConfigRepository;
    private readonly LicenseKeyService _licenseKeyService;
    private readonly PasswordService _passwordService;
    private readonly CodeGenerateService _codeGenerateService;

    public AdminService(
        IDbContext dbContext,
        StoreRepository storeRepository,
        ContractRepository contractRepository,
        LicenseKeyRepository licenseKeyRepository,
        LicenseLogRepository licenseLogRepository,
        NvrConfigRepository nvrConfigRepository,
        ChannelConfigRepository channelConfigRepository,
        LicenseKeyService licenseKeyService,
        PasswordService passwordService,
        CodeGenerateService codeGenerateService)
    {
        _dbContext = dbContext;
        _storeRepository = storeRepository;
        _contractRepository = contractRepository;
        _licenseKeyRepository = licenseKeyRepository;
        _licenseLogRepository = licenseLogRepository;
        _nvrConfigRepository = nvrConfigRepository;
        _channelConfigRepository = channelConfigRepository;
        _licenseKeyService = licenseKeyService;
        _passwordService = passwordService;
        _codeGenerateService = codeGenerateService;
    }

    /// <summary>
    /// 신규 매장을 등록한다.
    /// 
    /// 매장 ID는 백엔드에서 자동 생성한다.
    /// 최초 비밀번호는 매장 ID와 동일하게 저장한다.
    /// </summary>
    public async Task<ApiResponse<StoreCreateResponse>> CreateStoreAsync(StoreCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.StoreName))
        {
            return ApiResponse<StoreCreateResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "매장명을 입력해야 합니다.");
        }

        var storeId = await GenerateUniqueStoreIdAsync();

        var store = new Store
        {
            StoreId = storeId,
            StorePassword = _passwordService.CreateStorePasswordValue(storeId),
            StoreName = request.StoreName.Trim(),
            StoreBizNum = request.StoreBizNum?.Trim(),
            StoreStatus = (int)StoreStatus.Active
        };

        var storeCode = await _storeRepository.InsertAsync(store);

        var response = new StoreCreateResponse
        {
            StoreCode = storeCode,
            StoreId = storeId,
            InitialPassword = storeId,
            StoreName = store.StoreName
        };

        return ApiResponse<StoreCreateResponse>.Ok(
            response,
            "매장이 등록되었습니다. 최초 비밀번호는 매장 ID와 동일합니다.");
    }

    /// <summary>
    /// 신규 계약을 등록한다.
    /// 2026-05-16 미사용
    /// </summary>
    //public async Task<ApiResponse<ContractCreateResponse>> CreateContractAsync(ContractCreateRequest request)
    //{
    //    if (request.StoreCode <= 0)
    //    {
    //        return ApiResponse<ContractCreateResponse>.Fail(
    //            AuthErrorCode.InvalidStore,
    //            "매장 코드가 올바르지 않습니다.");
    //    }

    //    if (string.IsNullOrWhiteSpace(request.ContractNo))
    //    {
    //        return ApiResponse<ContractCreateResponse>.Fail(
    //            AuthErrorCode.ContractNotFound,
    //            "계약 번호를 입력해야 합니다.");
    //    }

    //    if (request.PccamCount < 0)
    //    {
    //        return ApiResponse<ContractCreateResponse>.Fail(
    //            AuthErrorCode.ContractSlotExceeded,
    //            "PC 캠 허용 수량은 0보다 작을 수 없습니다.");
    //    }

    //    if (request.ViewerCount < 0)
    //    {
    //        return ApiResponse<ContractCreateResponse>.Fail(
    //            AuthErrorCode.ContractSlotExceeded,
    //            "캠뷰어 허용 수량은 0보다 작을 수 없습니다.");
    //    }

    //    if (request.ContractType != ContractType.Purchase && request.EndDate == null)
    //    {
    //        return ApiResponse<ContractCreateResponse>.Fail(
    //            AuthErrorCode.ContractExpired,
    //            "테스트형 또는 구독형 계약은 종료일이 필요합니다.");
    //    }

    //    var store = await _storeRepository.GetByCodeAsync(request.StoreCode);

    //    if (store == null)
    //    {
    //        return ApiResponse<ContractCreateResponse>.Fail(
    //            AuthErrorCode.InvalidStore,
    //            "매장 정보를 찾을 수 없습니다.");
    //    }

    //    var existsContractNo = await _contractRepository.ExistsContractNoAsync(request.ContractNo);

    //    if (existsContractNo)
    //    {
    //        return ApiResponse<ContractCreateResponse>.Fail(
    //            AuthErrorCode.ContractNotFound,
    //            "이미 사용 중인 계약 번호입니다.");
    //    }

    //    var contract = new Contract
    //    {
    //        ConStore = request.StoreCode,
    //        ConNo = request.ContractNo.Trim(),
    //        ConType = (int)request.ContractType,
    //        ConPcc = request.PccamCount,
    //        ConView = request.ViewerCount,
    //        ConStart = request.StartDate,
    //        ConEnd = request.EndDate,
    //        Status = (int)ContractStatus.Active
    //    };

    //    var contractCode = await _contractRepository.InsertAsync(contract);

    //    var response = new ContractCreateResponse
    //    {
    //        ContractCode = contractCode,
    //        StoreCode = request.StoreCode,
    //        ContractNo = contract.ConNo
    //    };

    //    return ApiResponse<ContractCreateResponse>.Ok(response, "계약이 등록되었습니다.");
    //}

    /// <summary>
    /// 계약 기준으로 PC 캠 라이선스 키를 발급한다.
    /// 
    /// 발급 수량은 계약의 PC 캠 허용 수량을 초과할 수 없다.
    /// </summary>
    public async Task<ApiResponse<LicenseIssueResponse>> IssuePccamLicensesAsync(LicenseIssueRequest request)
    {
        if (request.ContractCode <= 0)
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

        using var connection = _dbContext.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            var contract = await _contractRepository.GetByCodeAsync(
                connection,
                transaction,
                request.ContractCode);

            if (contract == null)
            {
                transaction.Rollback();

                return ApiResponse<LicenseIssueResponse>.Fail(
                    AuthErrorCode.ContractNotFound,
                    "계약 정보를 찾을 수 없습니다.");
            }

            var issuedCount = await _licenseKeyRepository.CountByContractAsync(contract.ConCode);

            if (issuedCount + request.Count > contract.ConPcc)
            {
                transaction.Rollback();

                return ApiResponse<LicenseIssueResponse>.Fail(
                    AuthErrorCode.ContractSlotExceeded,
                    $"계약상 PC 캠 허용 수량({contract.ConPcc}개)을 초과할 수 없습니다.");
            }

            var issuedKeys = new List<string>();

            for (var i = 0; i < request.Count; i++)
            {
                var issueSequence = issuedCount + i + 1;

                var licenseKey = await GenerateUniqueLicenseKeyAsync(contract.ConCode, issueSequence);

                var license = new LicenseKey
                {
                    LicContract = contract.ConCode,
                    LicKey = licenseKey,
                    LicStatus = (int)LicenseStatus.Ready
                };

                var licenseCode = await _licenseKeyRepository.InsertAsync(
                    connection,
                    transaction,
                    license);

                var log = new LicenseLog
                {
                    LigCode = CreateLicenseLogCode(),
                    LigLicense = licenseCode,
                    LigStore = contract.ConStore,
                    LigHwid = "",
                    LigActionType = (int)LicenseActionType.Issue,
                    LigReason = "관리자 라이선스 발급"
                };

                await _licenseLogRepository.InsertAsync(
                    connection,
                    transaction,
                    log);

                issuedKeys.Add(licenseKey);
            }

            transaction.Commit();

            var response = new LicenseIssueResponse
            {
                ContractCode = contract.ConCode,
                LicenseKeys = issuedKeys
            };

            return ApiResponse<LicenseIssueResponse>.Ok(response, "PC 캠 라이선스 키가 발급되었습니다.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// NVR 설정을 저장한다.
    /// 
    /// 같은 매장에 설정이 이미 있으면 UPDATE,
    /// 없으면 INSERT 처리한다.
    /// </summary>
    public async Task<ApiResponse<bool>> SaveNvrConfigAsync(NvrConfigSaveRequest request)
    {
        var store = await _storeRepository.GetByCodeAsync(request.StoreCode);

        if (store == null)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidStore,
                "매장 정보를 찾을 수 없습니다.");
        }

        using var connection = _dbContext.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            var config = new NvrConfig
            {
                NvrStore = request.StoreCode,
                NvrId = request.NvrId.Trim(),
                NvrPassword = request.NvrPassword,
                NvrIp = request.NvrIp.Trim(),
                NvrPort = request.NvrPort,
                NvrChannels = request.NvrChannels,
                NvrVersion = string.IsNullOrWhiteSpace(request.NvrVersion)
                    ? CreateConfigVersion()
                    : request.NvrVersion.Trim()
            };

            await _nvrConfigRepository.UpsertAsync(connection, transaction, config);

            transaction.Commit();

            return ApiResponse<bool>.Ok(true, "NVR 설정이 저장되었습니다.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// POS 번호와 NVR 채널 매핑 정보를 저장한다.
    /// </summary>
    public async Task<ApiResponse<bool>> SaveChannelConfigAsync(ChannelConfigSaveRequest request)
    {
        var store = await _storeRepository.GetByCodeAsync(request.StoreCode);

        if (store == null)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidStore,
                "매장 정보를 찾을 수 없습니다.");
        }

        if (request.PosNo <= 0)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.ChannelConfigNotFound,
                "POS 번호가 올바르지 않습니다.");
        }

        if (request.ChannelNo <= 0)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.ChannelConfigNotFound,
                "채널 번호가 올바르지 않습니다.");
        }

        using var connection = _dbContext.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            var config = new ChannelConfig
            {
                ChnStore = request.StoreCode,
                ChnPos = request.PosNo,
                ChnCh = request.ChannelNo,
                ChnScreen = request.Screen
            };

            await _channelConfigRepository.UpsertAsync(connection, transaction, config);

            transaction.Commit();

            return ApiResponse<bool>.Ok(true, "채널 설정이 저장되었습니다.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 중복되지 않는 매장 ID를 백엔드에서 생성한다.
    /// 
    /// 매장 ID 형식:
    /// 영문 2자리 + 숫자 4자리
    /// 예: PC1000
    /// 
    /// DB는 생성하지 않고, 현재 최대값 조회와 중복 확인만 담당한다.
    /// </summary>
    private async Task<string> GenerateUniqueStoreIdAsync()
    {
        var currentMaxStoreId = await _storeRepository.GetMaxStoreIdAsync();

        var candidate = _codeGenerateService.CreateNextStoreId(currentMaxStoreId);

        for (var i = 0; i < 20; i++)
        {
            var exists = await _storeRepository.ExistsStoreIdAsync(candidate);

            if (!exists)
            {
                return candidate;
            }

            candidate = _codeGenerateService.IncrementStoreId(candidate);
        }

        throw new InvalidOperationException("중복되지 않는 매장 ID를 생성하지 못했습니다.");
    }

    /// <summary>
    /// DB에 중복되지 않는 PC 캠 라이선스 키를 생성한다.
    /// 
    /// 라이선스 키는 계약 기준으로 생성되며,
    /// 매장 연결 여부와 무관하다.
    /// </summary>
    private async Task<string> GenerateUniqueLicenseKeyAsync(
    int contractCode,
    int issueSequence)
    {
        for (var i = 0; i < 20; i++)
        {
            var key = _licenseKeyService.GeneratePccamLicenseKey(
                contractCode,
                issueSequence + i);

            var exists = await _licenseKeyRepository.ExistsKeyAsync(key);

            if (!exists)
            {
                return key;
            }
        }

        throw new InvalidOperationException("중복되지 않는 라이선스 키를 생성하지 못했습니다.");
    }

    /// <summary>
    /// licenselog.lig_code에 사용할 로그 코드를 생성한다.
    /// 
    /// 현재 DB 컬럼이 VARCHAR(20)이므로 20자 이하로 생성한다.
    /// 예: L260503123012123456
    /// </summary>
    private static string CreateLicenseLogCode()
    {
        var timePart = DateTime.UtcNow.ToString("yyMMddHHmmssfff");
        var randomPart = Random.Shared.Next(100, 999).ToString();

        return $"L{timePart}{randomPart}";
    }

    /// <summary>
    /// 설정 버전 문자열을 생성한다.
    /// 
    /// 캠뷰어 로컬 설정과 서버 설정 비교에 사용한다.
    /// </summary>
    private static string CreateConfigVersion()
    {
        return DateTime.UtcNow.ToString("yyyyMMddHHmmss");
    }
}