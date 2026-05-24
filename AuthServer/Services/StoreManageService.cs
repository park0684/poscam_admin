using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.Config;
using poscam.AuthServer.Models.Dtos.Store;
using poscam.AuthServer.Models.Dtos.Viewer;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Repositories;

namespace poscam.AuthServer.Services;

/// <summary>
/// 관리자/담당자용 매장 관리 서비스.
/// 
/// 매장 등록/수정, 목록 조회, 상세 조회,
/// 담당자 연결/해제를 담당한다.
/// 
/// 관리자와 담당자의 조회 범위 차이는 이 Service에서 판단한다.
/// </summary>
public class StoreManageService
{
    private readonly StoreRepository _storeRepository;
    private readonly StoreAssignmentRepository _storeAssignmentRepository;
    private readonly UserAccountRepository _userAccountRepository;
    private readonly PartnerRepository _partnerRepository;
    private readonly ContractRepository _contractRepository;
    private readonly LicenseKeyRepository _licenseKeyRepository;
    private readonly DeviceRepository _deviceRepository;
    private readonly NvrConfigRepository _nvrConfigRepository;
    private readonly ChannelConfigRepository _channelConfigRepository;
    private readonly PasswordService _passwordService;
    private readonly CodeGenerateService _codeGenerateService;
    private readonly AdminPermissionService _adminPermissionService;

    public StoreManageService(
        StoreRepository storeRepository,
        StoreAssignmentRepository storeAssignmentRepository,
        UserAccountRepository userAccountRepository,
        PartnerRepository partnerRepository,
        ContractRepository contractRepository,
        LicenseKeyRepository licenseKeyRepository,
        DeviceRepository deviceRepository,
        NvrConfigRepository nvrConfigRepository,
        ChannelConfigRepository channelConfigRepository,
        PasswordService passwordService,
        CodeGenerateService codeGenerateService,
        AdminPermissionService adminPermissionService)
    {
        _storeRepository = storeRepository;
        _storeAssignmentRepository = storeAssignmentRepository;
        _userAccountRepository = userAccountRepository;
        _partnerRepository = partnerRepository;
        _contractRepository = contractRepository;
        _licenseKeyRepository = licenseKeyRepository;
        _deviceRepository = deviceRepository;
        _nvrConfigRepository = nvrConfigRepository;
        _channelConfigRepository = channelConfigRepository;
        _passwordService = passwordService;
        _codeGenerateService = codeGenerateService;
        _adminPermissionService = adminPermissionService;
    }

    /// <summary>
    /// 매장 정보를 저장한다.
    /// 
    /// StoreCode가 없거나 0이면 신규 등록,
    /// StoreCode가 있으면 기존 매장 수정으로 처리한다.
    /// 
    /// 신규 등록 시:
    /// - 매장 ID는 백엔드에서 자동 생성
    /// - 최초 비밀번호는 매장 ID와 동일
    /// - 저장 후 StoreCode를 반환
    /// </summary>
    public async Task<ApiResponse<StoreSaveResponse>> SaveStoreAsync(StoreSaveRequest request, UserAccount loginUser)
    {
        if (string.IsNullOrWhiteSpace(request.StoreName))
        {
            return ApiResponse<StoreSaveResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "매장명을 입력해야 합니다.");
        }

        var isCreate = request.StoreCode == null || request.StoreCode <= 0;

        if (isCreate)
        {
            return await CreateStoreAsync(request, loginUser);
        }

        return await UpdateStoreAsync(request, loginUser);
    }

    /// <summary>
    /// 매장 목록을 조회한다.
    /// 
    /// 권한 정책:
    /// - System: 전체 매장 조회 가능
    /// - Admin: StoreManage 권한이 있어야 전체 매장 조회 가능
    /// - PartnerUser: 본인이 소속된 파트너사의 매장만 조회 가능
    /// 
    /// 검색 조건:
    /// - 매장 상태
    /// - 담당 파트너
    /// - 등록일 범위
    /// - 계약일 범위
    /// - 매장 ID / 매장명
    /// </summary>
    public async Task<ApiResponse<List<StoreListItemDto>>> GetStoreListAsync(
    UserAccount loginUser,
    StoreListSearchRequest request)
    {
        if (loginUser == null)
        {
            return ApiResponse<List<StoreListItemDto>>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        request ??= new StoreListSearchRequest();

        var validateResult = ValidateStoreListSearchRequest(request);

        if (!validateResult.Success)
        {
            return ApiResponse<List<StoreListItemDto>>.Fail(
                validateResult.ErrorCode,
                validateResult.Message);
        }

        var loginUserRole = (UserRole)loginUser.UserRole;

        List<StoreListItemDto> stores;

        if (loginUserRole == UserRole.System || loginUserRole == UserRole.Admin)
        {
            var permissionResult = await CheckStoreManagePermissionAsync(loginUser);

            if (!permissionResult.Success)
            {
                return ApiResponse<List<StoreListItemDto>>.Fail(
                    permissionResult.ErrorCode,
                    permissionResult.Message);
            }

            stores = await _storeRepository.GetListForAdminAsync(request);
        }
        else if (loginUserRole == UserRole.PartnerUser)
        {
            if (loginUser.PartnerCode == null || loginUser.PartnerCode <= 0)
            {
                return ApiResponse<List<StoreListItemDto>>.Fail(
                    AuthErrorCode.InvalidLogin,
                    "담당자 계정에 소속 파트너 정보가 없습니다.");
            }

            /*
             * 담당자는 본인이 지정된 매장이 아니라,
             * 본인이 소속된 파트너사의 매장 전체를 조회한다.
             *
             * 보안상 클라이언트에서 partnerCode를 전달하더라도 사용하지 않고,
             * 로그인 토큰에서 확인된 PartnerCode만 사용한다.
             */
            stores = await _storeRepository.GetListForPartnerAsync(
                loginUser.PartnerCode.Value,
                request);
        }
        else
        {
            return ApiResponse<List<StoreListItemDto>>.Fail(
                AuthErrorCode.PermissionDenied,
                "매장 목록을 조회할 권한이 없습니다.");
        }

        return ApiResponse<List<StoreListItemDto>>.Ok(
            stores,
            "매장 목록을 조회했습니다.");
    }

    /// <summary>
    /// 매장 목록 검색 조건을 검증한다.
    /// </summary>
    private static ApiResponse<bool> ValidateStoreListSearchRequest(
        StoreListSearchRequest request)
    {
        if (request.StoreStatus != null &&
            !Enum.IsDefined(typeof(StoreStatus), request.StoreStatus.Value))
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidStore,
                "매장 상태값이 올바르지 않습니다.");
        }

        if (request.RegisteredFrom != null &&
            request.RegisteredTo != null &&
            request.RegisteredFrom.Value.Date > request.RegisteredTo.Value.Date)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidStore,
                "등록일 시작일은 종료일보다 클 수 없습니다.");
        }

        if (request.ContractFrom != null &&
            request.ContractTo != null &&
            request.ContractFrom.Value.Date > request.ContractTo.Value.Date)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidStore,
                "계약일 시작일은 종료일보다 클 수 없습니다.");
        }

        return ApiResponse<bool>.Ok(true, "검색 조건이 올바릅니다.");
    }

    /// <summary>
    /// 매장 상세 정보를 조회한다.
    /// 
    /// 매장 상세 화면을 한 번에 구성할 수 있도록
    /// 매장 기본정보, 담당자 연결, 계약, 라이선스, 장비,
    /// NVR 설정, 채널 설정을 조합한다.
    /// 
    /// 권한 정책:
    /// - System: 허용
    /// - Admin: StoreManage 권한이 있어야 허용
    /// - PartnerUser: 본인 소속 파트너사에 연결된 매장만 조회 가능
    /// </summary>
    public async Task<ApiResponse<StoreDetailResponse>> GetStoreDetailAsync(
        int storeCode,
        UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return ApiResponse<StoreDetailResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        if (storeCode <= 0)
        {
            return ApiResponse<StoreDetailResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "매장 코드가 올바르지 않습니다.");
        }

        var loginUserRole = (UserRole)loginUser.UserRole;

        // System / Admin은 StoreManage 권한 확인
        if (loginUserRole == UserRole.System ||
            loginUserRole == UserRole.Admin)
        {
            var permissionResult = await CheckStoreManagePermissionAsync(
                loginUser);

            if (!permissionResult.Success)
            {
                return ApiResponse<StoreDetailResponse>.Fail(
                    permissionResult.ErrorCode,
                    permissionResult.Message);
            }
        }

        // 현재 로그인 사용자가 해당 매장에 접근 가능한지 확인
        var canAccess = await CanAccessStoreAsync(
            storeCode,
            loginUser);

        if (!canAccess)
        {
            return ApiResponse<StoreDetailResponse>.Fail(
                AuthErrorCode.PermissionDenied,
                "해당 매장을 조회할 권한이 없습니다.");
        }

        var store = await _storeRepository.GetDetailBaseAsync(storeCode);

        if (store == null)
        {
            return ApiResponse<StoreDetailResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "매장 정보를 찾을 수 없습니다.");
        }

        var assignments = await _storeAssignmentRepository.GetByStoreAsync(storeCode);
        var contracts = await _contractRepository.GetByStoreAsync(storeCode);
        var licenses = await _licenseKeyRepository.GetByStoreAsync(storeCode);
        var devices = await _deviceRepository.GetByStoreAsync(storeCode);
        var nvrConfig = await _nvrConfigRepository.GetByStoreAsync(storeCode);
        var channelConfigs = await _channelConfigRepository.GetByStoreAsync(storeCode);

        var response = new StoreDetailResponse
        {
            Store = store,
            Assignments = assignments,
            Contracts = contracts,
            Licenses = licenses,

            Devices = new StoreDeviceGroupDto
            {
                Pccams = devices
                    .Where(x => x.AppType == (int)DeviceAppType.Pccam)
                    .ToList(),

                Viewers = devices
                    .Where(x => x.AppType == (int)DeviceAppType.Viewer)
                    .ToList()
            },

            NvrConfig = nvrConfig == null
                ? null
                : new ManageNvrConfigDto
                {
                    NvrId = nvrConfig.NvrId,
                    HasPassword = !string.IsNullOrWhiteSpace(nvrConfig.NvrPassword),
                    NvrIp = nvrConfig.NvrIp,
                    NvrPort = nvrConfig.NvrPort,
                    NvrChannels = nvrConfig.NvrChannels,
                    NvrVersion = nvrConfig.NvrVersion
                },

            ChannelConfigs = channelConfigs
                .Select(x => new ChannelConfigDto
                {
                    PosNo = x.ChnPos,
                    ChannelNo = x.ChnCh,
                    Screen = x.ChnScreen
                })
                .ToList()
        };

        return ApiResponse<StoreDetailResponse>.Ok(
            response,
            "매장 상세 정보를 조회했습니다.");
    }

    /// <summary>
    /// 매장에 담당자를 연결한다.
    /// 
    /// 연결된 담당자는 해당 매장을 조회할 수 있다.
    /// 담당자 역할은 SALES, INSTALL, MANAGE, CONTRACT, SUPPORT, ETC 중 하나를 사용한다.
    /// 
    /// 권한 정책:
    /// - System: 허용
    /// - Admin: StoreManage 권한이 있어야 허용
    /// - PartnerUser: 본인 소속 파트너사에 연결된 매장만 허용
    /// </summary>
    public async Task<ApiResponse<StoreAssignmentResponse>> AddAssignmentAsync(
        StoreAssignmentCreateRequest request,
        UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return ApiResponse<StoreAssignmentResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        if (request.StoreCode <= 0)
        {
            return ApiResponse<StoreAssignmentResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "매장 코드가 올바르지 않습니다.");
        }

        if (request.UserCode <= 0)
        {
            return ApiResponse<StoreAssignmentResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "담당자 코드가 올바르지 않습니다.");
        }

        if (!AssignmentRoles.IsValid(request.AssignmentRole))
        {
            return ApiResponse<StoreAssignmentResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "담당 역할이 올바르지 않습니다.");
        }

        var store = await _storeRepository.GetByCodeAsync(request.StoreCode);

        if (store == null)
        {
            return ApiResponse<StoreAssignmentResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "매장 정보를 찾을 수 없습니다.");
        }

        var user = await _userAccountRepository.GetByCodeAsync(request.UserCode);

        if (user == null)
        {
            return ApiResponse<StoreAssignmentResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "담당자 정보를 찾을 수 없습니다.");
        }

        if (user.UserStatus != (int)UserStatus.Active)
        {
            return ApiResponse<StoreAssignmentResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "승인되지 않았거나 사용할 수 없는 담당자입니다.");
        }

        var loginUserRole = (UserRole)loginUser.UserRole;

        // 담당자 계정은 본인 권한 범위 내 매장만 담당자 연결 가능
        if (loginUserRole == UserRole.PartnerUser)
        {
            var canManage = await CanManageStoreAsync(
                request.StoreCode,
                loginUser);

            if (!canManage)
            {
                return ApiResponse<StoreAssignmentResponse>.Fail(
                    AuthErrorCode.PermissionDenied,
                    "해당 매장의 담당자를 지정할 권한이 없습니다.");
            }

            if (loginUser.PartnerCode == null ||
                loginUser.PartnerCode <= 0)
            {
                return ApiResponse<StoreAssignmentResponse>.Fail(
                    AuthErrorCode.InvalidLogin,
                    "담당자 계정에 파트너사가 지정되어 있지 않습니다.");
            }

            // 담당자 계정은 연결 대상 파트너사를
            // 본인이 소속된 파트너사로 강제한다.
            request.PartnerCode = loginUser.PartnerCode.Value;

            var isValidUser = await IsUserInPartnerAsync(
                request.UserCode,
                loginUser.PartnerCode.Value);

            if (!isValidUser)
            {
                return ApiResponse<StoreAssignmentResponse>.Fail(
                    AuthErrorCode.PermissionDenied,
                    "본인 파트너사 내 담당자만 지정할 수 있습니다.");
            }
        }
        // System / Admin은 StoreManage 권한을 확인한다.
        else if (
            loginUserRole == UserRole.System ||
            loginUserRole == UserRole.Admin)
        {
            var permissionResult = await CheckStoreManagePermissionAsync(
                loginUser);

            if (!permissionResult.Success)
            {
                return ApiResponse<StoreAssignmentResponse>.Fail(
                    permissionResult.ErrorCode,
                    permissionResult.Message);
            }
        }
        else
        {
            return ApiResponse<StoreAssignmentResponse>.Fail(
                AuthErrorCode.PermissionDenied,
                "담당자를 지정할 권한이 없습니다.");
        }

        var partnerCode = request.PartnerCode ?? user.PartnerCode;

        if (partnerCode != null)
        {
            var partner = await _partnerRepository.GetByCodeAsync(
                partnerCode.Value);

            if (partner == null)
            {
                return ApiResponse<StoreAssignmentResponse>.Fail(
                    AuthErrorCode.InvalidStore,
                    "파트너사 정보를 찾을 수 없습니다.");
            }
        }

        var exists = await _storeAssignmentRepository.ExistsActiveAssignmentAsync(
            request.StoreCode,
            request.UserCode,
            request.AssignmentRole);

        if (exists)
        {
            return ApiResponse<StoreAssignmentResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "이미 동일한 담당 역할로 연결된 담당자입니다.");
        }

        if (request.IsPrimary)
        {
            await _storeAssignmentRepository.ClearPrimaryByStoreAsync(
                request.StoreCode);
        }

        var assignment = new StoreUserAssignment
        {
            StoreCode = request.StoreCode,
            UserCode = request.UserCode,
            PartnerCode = partnerCode,
            AssignmentRole = request.AssignmentRole.Trim().ToUpperInvariant(),
            IsPrimary = request.IsPrimary,
            Status = (int)AssignmentStatus.Active,

            // 실제 연결 실행자 저장
            AssignedBy = loginUser.UserCode
        };

        var assignmentCode = await _storeAssignmentRepository.InsertAsync(
            assignment);

        var response = new StoreAssignmentResponse
        {
            AssignmentCode = assignmentCode,
            StoreCode = request.StoreCode,
            UserCode = request.UserCode,
            AssignmentRole = assignment.AssignmentRole,
            Saved = true
        };

        return ApiResponse<StoreAssignmentResponse>.Ok(
            response,
            "매장 담당자가 연결되었습니다.");
    }

    /// <summary>
    /// 매장 담당자 연결을 해제한다.
    /// 
    /// 권한 정책:
    /// - System: 허용
    /// - Admin: StoreManage 권한이 있어야 허용
    /// - PartnerUser:
    ///   본인 소속 파트너사에 연결된 매장에 대해서만 처리 가능하며,
    ///   본인 파트너사 소속 담당자 연결만 해제할 수 있다.
    /// </summary>
    public async Task<ApiResponse<bool>> ReleaseAssignmentAsync(
        int assignmentCode,
        UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        if (assignmentCode <= 0)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidStore,
                "담당자 연결 코드가 올바르지 않습니다.");
        }

        var assignment = await _storeAssignmentRepository.GetByCodeAsync(
            assignmentCode);

        if (assignment == null)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidStore,
                "담당자 연결 정보를 찾을 수 없습니다.");
        }

        var loginUserRole = (UserRole)loginUser.UserRole;

        // System / Admin은 StoreManage 권한 확인
        if (
            loginUserRole == UserRole.System ||
            loginUserRole == UserRole.Admin)
        {
            var permissionResult = await CheckStoreManagePermissionAsync(
                loginUser);

            if (!permissionResult.Success)
            {
                return ApiResponse<bool>.Fail(
                    permissionResult.ErrorCode,
                    permissionResult.Message);
            }
        }
        // 담당자는 본인 파트너사 범위 내 연결만 해제 가능
        else if (loginUserRole == UserRole.PartnerUser)
        {
            var canManage = await CanManageStoreAsync(
                assignment.StoreCode,
                loginUser);

            if (!canManage)
            {
                return ApiResponse<bool>.Fail(
                    AuthErrorCode.PermissionDenied,
                    "해당 매장의 담당자 연결을 해제할 권한이 없습니다.");
            }

            if (loginUser.PartnerCode == null ||
                loginUser.PartnerCode <= 0)
            {
                return ApiResponse<bool>.Fail(
                    AuthErrorCode.InvalidLogin,
                    "담당자 계정에 파트너사가 지정되어 있지 않습니다.");
            }

            if (assignment.PartnerCode != loginUser.PartnerCode.Value)
            {
                return ApiResponse<bool>.Fail(
                    AuthErrorCode.PermissionDenied,
                    "본인 파트너사의 담당자 연결만 해제할 수 있습니다.");
            }
        }
        else
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.PermissionDenied,
                "담당자 연결 해제 권한이 없습니다.");
        }

        var affected = await _storeAssignmentRepository.ReleaseAsync(
            assignmentCode);

        if (affected <= 0)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidStore,
                "담당자 연결 해제가 처리되지 않았습니다.");
        }

        return ApiResponse<bool>.Ok(
            true,
            "담당자 연결이 해제되었습니다.");
    }

    /// <summary>
    /// 신규 매장을 등록한다.
    /// 
    /// 권한 정책:
    /// - System: 허용
    /// - Admin: StoreManage 권한이 있어야 허용
    /// - PartnerUser: 본인 소속 파트너사 기준으로 등록 가능
    /// 
    /// 처리 순서:
    /// 1. 로그인 및 권한 확인
    /// 2. 대표 파트너사 결정
    /// 3. 대표 담당자 유효성 검증
    /// 4. 매장 ID 생성
    /// 5. 매장 등록
    /// 6. 필요 시 대표 담당자 연결 등록
    /// </summary>
    private async Task<ApiResponse<StoreSaveResponse>> CreateStoreAsync(
        StoreSaveRequest request,
        UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return ApiResponse<StoreSaveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        var loginUserRole = (UserRole)loginUser.UserRole;

        // System / Admin은 StoreManage 권한 확인
        if (loginUserRole == UserRole.System ||
            loginUserRole == UserRole.Admin)
        {
            var permissionResult = await CheckStoreManagePermissionAsync(
                loginUser);

            if (!permissionResult.Success)
            {
                return ApiResponse<StoreSaveResponse>.Fail(
                    permissionResult.ErrorCode,
                    permissionResult.Message);
            }
        }
        // PartnerUser 외 다른 역할은 등록 불가
        else if (loginUserRole != UserRole.PartnerUser)
        {
            return ApiResponse<StoreSaveResponse>.Fail(
                AuthErrorCode.PermissionDenied,
                "매장을 등록할 권한이 없습니다.");
        }

        // 1. 대표 파트너사 결정
        var primaryPartnerCodeResult = await ResolvePrimaryPartnerCodeAsync(
            request,
            loginUser);

        if (!primaryPartnerCodeResult.Success)
        {
            return ApiResponse<StoreSaveResponse>.Fail(
                primaryPartnerCodeResult.ErrorCode,
                primaryPartnerCodeResult.Message);
        }

        var primaryPartnerCode = primaryPartnerCodeResult.PartnerCode;

        // 2. 대표 담당자 결정
        var primaryManagerUserCode = request.PrimaryManagerUserCode;

        // 담당자가 매장을 등록하는 경우,
        // 대표 담당자가 지정되지 않으면 본인을 기본 담당자로 사용한다.
        if (loginUserRole == UserRole.PartnerUser)
        {
            if (primaryManagerUserCode == null ||
                primaryManagerUserCode <= 0)
            {
                primaryManagerUserCode = loginUser.UserCode;
            }
        }

        // 3. 대표 파트너사와 대표 담당자가 함께 지정된 경우
        // 해당 담당자가 그 파트너사 소속인지 검증한다.
        if (primaryPartnerCode != null &&
            primaryManagerUserCode != null)
        {
            var isValidManager = await IsUserInPartnerAsync(
                primaryManagerUserCode.Value,
                primaryPartnerCode.Value);

            if (!isValidManager)
            {
                return ApiResponse<StoreSaveResponse>.Fail(
                    AuthErrorCode.InvalidLogin,
                    "선택한 담당자가 해당 파트너사 소속이 아닙니다.");
            }
        }

        // 4. 모든 사전 검증이 완료된 뒤 매장 ID 생성
        var storeId = await GenerateUniqueStoreIdAsync();

        // 5. 매장 엔티티 생성
        var store = new Store
        {
            StoreId = storeId,
            StorePassword = _passwordService.CreateStorePasswordValue(storeId),
            StoreName = request.StoreName.Trim(),
            StoreBizNum = request.StoreBizNum?.Trim(),
            StoreOwnerName = request.StoreOwnerName?.Trim(),
            StoreTel = request.StoreTel?.Trim(),
            StoreEmail = request.StoreEmail?.Trim(),
            StoreZipcode = request.StoreZipcode?.Trim(),
            StoreAddress1 = request.StoreAddress1?.Trim(),
            StoreAddress2 = request.StoreAddress2?.Trim(),
            StoreMemo = request.StoreMemo?.Trim(),
            StoreStatus = request.StoreStatus ?? (int)StoreStatus.Active
        };

        // 6. 매장 저장
        var storeCode = await _storeRepository.InsertAsync(store);

        // 7. 대표 파트너사와 대표 담당자가 모두 존재하면 기본 담당자 연결 생성
        if (primaryPartnerCode != null &&
            primaryManagerUserCode != null)
        {
            var assignment = new StoreUserAssignment
            {
                StoreCode = storeCode,
                UserCode = primaryManagerUserCode.Value,
                PartnerCode = primaryPartnerCode.Value,
                AssignmentRole = "MANAGE",
                IsPrimary = true,
                Status = (int)AssignmentStatus.Active,
                AssignedBy = loginUser.UserCode
            };

            await _storeAssignmentRepository.InsertAsync(assignment);
        }

        var response = new StoreSaveResponse
        {
            StoreCode = storeCode,
            StoreId = storeId,
            InitialPassword = storeId,
            StoreName = store.StoreName,
            Created = true,
            Saved = true
        };

        return ApiResponse<StoreSaveResponse>.Ok(
            response,
            "매장이 등록되었습니다. 최초 비밀번호는 매장 ID와 동일합니다.");
    }

    /// <summary>
    /// 기존 매장 기본정보를 수정한다.
    /// 
    /// 권한 정책:
    /// - System: 허용
    /// - Admin: StoreManage 권한이 있어야 허용
    /// - PartnerUser: 본인 소속 파트너사에 연결된 매장만 수정 가능
    /// </summary>
    private async Task<ApiResponse<StoreSaveResponse>> UpdateStoreAsync(
        StoreSaveRequest request,
        UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return ApiResponse<StoreSaveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        if (request.StoreCode == null || request.StoreCode <= 0)
        {
            return ApiResponse<StoreSaveResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "수정할 매장 코드가 올바르지 않습니다.");
        }

        var storeCode = request.StoreCode.Value;

        var existingStore = await _storeRepository.GetByCodeAsync(storeCode);

        if (existingStore == null)
        {
            return ApiResponse<StoreSaveResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "수정할 매장 정보를 찾을 수 없습니다.");
        }

        // 현재 로그인 사용자가 해당 매장을 수정할 수 있는지 확인
        var canManage = await CanManageStoreAsync(
            storeCode,
            loginUser);

        if (!canManage)
        {
            return ApiResponse<StoreSaveResponse>.Fail(
                AuthErrorCode.PermissionDenied,
                "해당 매장의 정보를 수정할 권한이 없습니다.");
        }

        existingStore.StoreName = request.StoreName.Trim();
        existingStore.StoreBizNum = request.StoreBizNum?.Trim();
        existingStore.StoreOwnerName = request.StoreOwnerName?.Trim();
        existingStore.StoreTel = request.StoreTel?.Trim();
        existingStore.StoreEmail = request.StoreEmail?.Trim();
        existingStore.StoreZipcode = request.StoreZipcode?.Trim();
        existingStore.StoreAddress1 = request.StoreAddress1?.Trim();
        existingStore.StoreAddress2 = request.StoreAddress2?.Trim();
        existingStore.StoreMemo = request.StoreMemo?.Trim();

        if (request.StoreStatus != null)
        {
            // 필요 시 StoreStatus 허용값 검증을 별도 추가할 수 있다.
            existingStore.StoreStatus = request.StoreStatus.Value;
        }

        var affected = await _storeRepository.UpdateAsync(existingStore);

        if (affected <= 0)
        {
            return ApiResponse<StoreSaveResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "매장 정보가 수정되지 않았습니다.");
        }

        var response = new StoreSaveResponse
        {
            StoreCode = existingStore.StoreCode,
            StoreId = existingStore.StoreId,

            // 수정 시 최초 비밀번호는 반환하지 않는다.
            InitialPassword = null,

            StoreName = existingStore.StoreName,
            Created = false,
            Saved = true
        };

        return ApiResponse<StoreSaveResponse>.Ok(
            response,
            "매장 정보가 수정되었습니다.");
    }

    /// <summary>
    /// 현재 로그인 사용자가 특정 매장에 접근 가능한지 확인한다.
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

        //system / 관리자는 모든 매장 접근 가능
        if (loginUserRole == UserRole.Admin || loginUserRole == UserRole.System)
        {
            return true;
        }

        // 담당자는 본인 소속 파트너사에 연결된 매장만 접근 가능
        if (loginUserRole == UserRole.PartnerUser)
        {
            if (loginUser.PartnerCode == null || loginUser.PartnerCode <= 0)
            {
                return false;
            }

            return await _storeRepository.CanPartnerAccessStoreAsync(
                loginUser.PartnerCode.Value,
            storeCode);
        }

        return false;
    }

    /// <summary>
    /// 중복되지 않는 매장 ID를 백엔드에서 생성한다.
    /// 
    /// 매장 ID 형식:
    /// 영문 2자리 + 숫자 4자리
    /// 예: PC1000
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
    /// 현재 로그인 사용자가 특정 매장을 관리할 수 있는지 확인한다.
    /// 
    /// 권한 정책:
    /// - System: 모든 매장 관리 가능
    /// - Admin: StoreManage 권한이 있어야 관리 가능
    /// - PartnerUser: 본인 소속 파트너사에 연결된 매장만 관리 가능
    /// </summary>
    private async Task<bool> CanManageStoreAsync(
        int storeCode,
        UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return false;
        }

        var loginUserRole = (UserRole)loginUser.UserRole;

        // System / Admin은 매장 관리 권한을 확인한다.
        // - System은 AdminPermissionService 내부에서 자동 허용
        // - Admin은 StoreManage 권한 보유 여부를 확인
        if (loginUserRole == UserRole.System ||
            loginUserRole == UserRole.Admin)
        {
            var permissionResult = await CheckStoreManagePermissionAsync(
                loginUser);

            return permissionResult.Success;
        }

        // 담당자는 본인 소속 파트너사에 연결된 매장만 관리 가능
        if (loginUserRole == UserRole.PartnerUser)
        {
            return await CanAccessStoreAsync(
                storeCode,
                loginUser);
        }

        return false;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="userCode"></param>
    /// <param name="partnerCode"></param>
    /// <returns></returns>
    private async Task<bool> IsUserInPartnerAsync(
        int userCode,
        int partnerCode)
    {
        var user = await _userAccountRepository.GetByCodeAsync(userCode);

        return user != null
               && user.UserRole == (int)UserRole.PartnerUser
               && user.UserStatus == (int)UserStatus.Active
               && user.PartnerCode == partnerCode;
    }

    /// <summary>
    /// 매장 사용 현황을 조회한다.
    /// 
    /// 권한 정책:
    /// - System: 허용
    /// - Admin: StoreManage 권한이 있어야 허용
    /// - PartnerUser: 본인 소속 파트너사에 연결된 매장만 허용
    /// </summary>
    public async Task<ApiResponse<StoreUsageSummaryDto>> GetUsageSummaryAsync(
        int storeCode,
        UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return ApiResponse<StoreUsageSummaryDto>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        if (storeCode <= 0)
        {
            return ApiResponse<StoreUsageSummaryDto>.Fail(
                AuthErrorCode.InvalidStore,
                "매장 코드가 올바르지 않습니다.");
        }

        var loginUserRole = (UserRole)loginUser.UserRole;

        // System / Admin은 StoreManage 권한 확인
        if (
            loginUserRole == UserRole.System ||
            loginUserRole == UserRole.Admin)
        {
            var permissionResult = await CheckStoreManagePermissionAsync(
                loginUser);

            if (!permissionResult.Success)
            {
                return ApiResponse<StoreUsageSummaryDto>.Fail(
                    permissionResult.ErrorCode,
                    permissionResult.Message);
            }
        }

        var canAccess = await CanAccessStoreAsync(
            storeCode,
            loginUser);

        if (!canAccess)
        {
            return ApiResponse<StoreUsageSummaryDto>.Fail(
                AuthErrorCode.PermissionDenied,
                "해당 매장의 사용 현황을 조회할 권한이 없습니다.");
        }

        var summary = await _storeRepository.GetUsageSummaryAsync(storeCode);

        return ApiResponse<StoreUsageSummaryDto>.Ok(
            summary,
            "매장 사용 현황을 조회했습니다.");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="loginUser"></param>
    /// <returns></returns>
    private async Task<PrimaryPartnerResolveResult> ResolvePrimaryPartnerCodeAsync(
        StoreSaveRequest request,
        UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return PrimaryPartnerResolveResult.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        var loginUserRole = (UserRole)loginUser.UserRole;

        if (loginUserRole == UserRole.System || loginUserRole == UserRole.Admin)
        {
            if (request.PrimaryPartnerCode == null || request.PrimaryPartnerCode <= 0)
            {
                return PrimaryPartnerResolveResult.Ok(null);
            }

            var partner = await _partnerRepository.GetByCodeAsync(
                request.PrimaryPartnerCode.Value);

            if (partner == null)
            {
                return PrimaryPartnerResolveResult.Fail(
                    AuthErrorCode.InvalidStore,
                    "파트너사 정보를 찾을 수 없습니다.");
            }

            if (partner.PartnerStatus != (int)PartnerStatus.Active)
            {
                return PrimaryPartnerResolveResult.Fail(
                    AuthErrorCode.InvalidStore,
                    "사용할 수 없는 파트너사입니다.");
            }

            return PrimaryPartnerResolveResult.Ok(request.PrimaryPartnerCode.Value);
        }

        if (loginUserRole == UserRole.PartnerUser)
        {
            if (loginUser.PartnerCode == null || loginUser.PartnerCode <= 0 )
            {
                return PrimaryPartnerResolveResult.Fail(
                    AuthErrorCode.InvalidLogin,
                    "담당자 계정에 파트너사가 지정되어 있지 않습니다.");
            }

            return PrimaryPartnerResolveResult.Ok(loginUser.PartnerCode.Value);
        }

        return PrimaryPartnerResolveResult.Fail(
            AuthErrorCode.PermissionDenied,
            "매장 등록 권한이 없습니다.");
    }

    private class PrimaryPartnerResolveResult
    {
        public bool Success { get; set; }

        public AuthErrorCode ErrorCode { get; set; }

        public string Message { get; set; } = "";

        public int? PartnerCode { get; set; }

        public static PrimaryPartnerResolveResult Ok(int? partnerCode)
        {
            return new PrimaryPartnerResolveResult
            {
                Success = true,
                ErrorCode = AuthErrorCode.None,
                PartnerCode = partnerCode
            };
        }

        public static PrimaryPartnerResolveResult Fail(
            AuthErrorCode errorCode,
            string message)
        {
            return new PrimaryPartnerResolveResult
            {
                Success = false,
                ErrorCode = errorCode,
                Message = message
            };
        }
    }

    /// <summary>
    /// 매장 관리 권한을 확인한다.
    /// 
    /// System은 자동 허용되고,
    /// Admin은 StoreManage 권한을 보유해야 한다.
    /// </summary>
    private Task<ApiResponse<bool>> CheckStoreManagePermissionAsync(
        UserAccount loginUser)
    {
        return _adminPermissionService.CheckPermissionAsync(
            loginUser,
            AdminPermissionType.StoreManage);
    }
}