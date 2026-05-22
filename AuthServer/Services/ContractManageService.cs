using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.Contract;
using poscam.AuthServer.Models.Dtos.Store;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Repositories;

namespace poscam.AuthServer.Services;

/// <summary>
/// 관리자/담당자용 계약 관리 서비스.
/// 
/// 계약 목록 조회, 신규 계약 등록, 기존 계약 수정을 담당한다.
/// System은 모든 계약 관리가 가능하고,
/// Admin은 ContractManage 권한을 보유한 경우 계약 관리가 가능하다.
/// 담당자는 본인 소속 파트너사 또는 담당 매장 범위 안에서
/// 계약 조회와 등록/수정 기능을 사용할 수 있다.
/// </summary>
public class ContractManageService
{
    private readonly StoreRepository _storeRepository;
    private readonly StoreAssignmentRepository _storeAssignmentRepository;
    private readonly ContractRepository _contractRepository;
    private readonly CodeGenerateService _codeGenerateService;
    private readonly AdminPermissionService _adminPermissionService;

    public ContractManageService(
        StoreRepository storeRepository,
        StoreAssignmentRepository storeAssignmentRepository,
        ContractRepository contractRepository,
        CodeGenerateService codeGenerateService,
        AdminPermissionService adminPermissionService)
    {
        _storeRepository = storeRepository;
        _storeAssignmentRepository = storeAssignmentRepository;
        _contractRepository = contractRepository;
        _codeGenerateService = codeGenerateService;
        _adminPermissionService = adminPermissionService;
    }

    /// <summary>
    /// 매장별 계약 목록을 조회한다.
    /// 
    /// 권한 정책:
    /// - System: 허용
    /// - Admin: ContractManage 권한이 있어야 허용
    /// - PartnerUser: 본인 소속 파트너사에 연결된 매장의 계약만 조회 가능
    /// </summary>
    public async Task<ApiResponse<List<StoreContractDto>>> GetContractsByStoreAsync(
        int storeCode,
        UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return ApiResponse<List<StoreContractDto>>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        if (storeCode <= 0)
        {
            return ApiResponse<List<StoreContractDto>>.Fail(
                AuthErrorCode.InvalidStore,
                "매장 코드가 올바르지 않습니다.");
        }

        var loginUserRole = (UserRole)loginUser.UserRole;

        // System / Admin은 계약 관리 권한 확인
        if (loginUserRole == UserRole.System ||
            loginUserRole == UserRole.Admin)
        {
            var permissionResult = await CheckContractManagePermissionAsync(
                loginUser);

            if (!permissionResult.Success)
            {
                return ApiResponse<List<StoreContractDto>>.Fail(
                    permissionResult.ErrorCode,
                    permissionResult.Message);
            }
        }

        var canAccess = await CanAccessStoreAsync(
            storeCode,
            loginUser);

        if (!canAccess)
        {
            return ApiResponse<List<StoreContractDto>>.Fail(
                AuthErrorCode.PermissionDenied,
                "해당 매장의 계약 정보를 조회할 권한이 없습니다.");
        }

        var contracts = await _contractRepository.GetByStoreAsync(storeCode);

        return ApiResponse<List<StoreContractDto>>.Ok(
            contracts,
            "계약 목록을 조회했습니다.");
    }

    /// <summary>
    /// 계약 정보를 저장한다.
    /// 
    /// ContractCode가 없으면 신규 등록,
    /// ContractCode가 있으면 기존 계약 수정으로 처리한다.
    /// 
    /// 권한 정책:
    /// - System: 허용
    /// - Admin: ContractManage 권한이 있어야 허용
    /// - PartnerUser: 본인 소속 파트너사에 연결된 매장의 계약만 저장 가능
    /// </summary>
    public async Task<ApiResponse<ContractSaveResponse>> SaveContractAsync(
        ContractSaveRequest request,
        UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return ApiResponse<ContractSaveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        var loginUserRole = (UserRole)loginUser.UserRole;

        // System / Admin은 계약 관리 권한 확인
        if (loginUserRole == UserRole.System ||
            loginUserRole == UserRole.Admin)
        {
            var permissionResult = await CheckContractManagePermissionAsync(
                loginUser);

            if (!permissionResult.Success)
            {
                return ApiResponse<ContractSaveResponse>.Fail(
                    permissionResult.ErrorCode,
                    permissionResult.Message);
            }
        }
        else if (loginUserRole != UserRole.PartnerUser)
        {
            return ApiResponse<ContractSaveResponse>.Fail(
                AuthErrorCode.PermissionDenied,
                "계약을 저장할 권한이 없습니다.");
        }

        if (request.StoreCode <= 0)
        {
            return ApiResponse<ContractSaveResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "매장 코드가 올바르지 않습니다.");
        }

        if (request.PccamCount < 0)
        {
            return ApiResponse<ContractSaveResponse>.Fail(
                AuthErrorCode.ContractSlotExceeded,
                "PC캠 허용 수량은 0보다 작을 수 없습니다.");
        }

        if (request.ViewerCount < 0)
        {
            return ApiResponse<ContractSaveResponse>.Fail(
                AuthErrorCode.ContractSlotExceeded,
                "캠뷰어 허용 수량은 0보다 작을 수 없습니다.");
        }

        // 1. 매장 존재 확인
        var store = await _storeRepository.GetByCodeAsync(request.StoreCode);

        if (store == null)
        {
            return ApiResponse<ContractSaveResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "매장 정보를 찾을 수 없습니다.");
        }

        // 2. 해당 매장에 접근 가능한 사용자인지 확인
        var canAccessStore = await CanAccessStoreAsync(
            request.StoreCode,
            loginUser);

        if (!canAccessStore)
        {
            return ApiResponse<ContractSaveResponse>.Fail(
                AuthErrorCode.PermissionDenied,
                "해당 매장의 계약을 저장할 권한이 없습니다.");
        }

        // 3. 매장의 대표 담당 파트너사 조회
        var primaryPartnerCode =
            await _storeAssignmentRepository.GetPrimaryPartnerCodeByStoreAsync(
                request.StoreCode);

        if (primaryPartnerCode == null)
        {
            return ApiResponse<ContractSaveResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "해당 매장에 대표 담당 파트너사가 지정되어 있지 않습니다.");
        }

        // 4. 담당자는 자기 파트너사가 담당하는 매장에만 계약 등록/수정 가능
        if (loginUserRole == UserRole.PartnerUser)
        {
            if (!loginUser.PartnerCode.HasValue ||
                loginUser.PartnerCode.Value != primaryPartnerCode.Value)
            {
                return ApiResponse<ContractSaveResponse>.Fail(
                    AuthErrorCode.PermissionDenied,
                    "해당 매장의 담당 파트너사가 아니므로 계약을 저장할 수 없습니다.");
            }
        }

        var isCreate =
            request.ContractCode == null ||
            request.ContractCode <= 0;

        if (isCreate)
        {
            return await CreateContractAsync(
                request,
                primaryPartnerCode.Value);
        }

        return await UpdateContractAsync(
            request,
            primaryPartnerCode.Value);
    }

    /// <summary>
    /// 신규 계약을 등록한다.
    /// 
    /// 계약번호는 백엔드에서 자동 생성한다.
    /// 매장 연결 계약의 파트너사 코드는
    /// 상위 서비스에서 검증 후 전달받은 partnerCode를 사용한다.
    /// 
    /// 주의:
    /// - request 값에 있는 파트너코드를 신뢰하지 않는다.
    /// - 관리자 등록이든 파트너 담당자 등록이든,
    ///   계약의 소유 파트너사는 서비스에서 확정한 partnerCode로 저장한다.
    /// </summary>
    private async Task<ApiResponse<ContractSaveResponse>> CreateContractAsync(
        ContractSaveRequest request,
        int partnerCode)
    {
        var startDate = ResolveStartDateForCreate(
            request.ContractType,
            request.StartDate);

        var endDateResult = ResolveEndDate(
            request.ContractType,
            startDate,
            request.EndDate);

        if (!endDateResult.Success)
        {
            return ApiResponse<ContractSaveResponse>.Fail(
                endDateResult.ErrorCode,
                endDateResult.Message);
        }

        // 계약번호는 계약 소유 파트너사 기준으로 생성한다.
        var contractNo = await GenerateUniqueContractNoAsync(
            request.ContractType,
            partnerCode);

        var contract = new Contract
        {
            // 매장 상세에서 등록하는 계약이므로 매장 연결
            ConStore = request.StoreCode,

            // 계약의 실제 소유 파트너사
            // 상위 서비스에서 확정한 값만 사용한다.
            ConPartner = partnerCode,

            ConNo = contractNo,
            ConType = (int)request.ContractType,
            ConPcc = request.PccamCount,
            ConView = request.ViewerCount,
            ConStart = startDate,
            ConEnd = endDateResult.EndDate,
            Status = request.Status ?? (int)ContractStatus.Active
        };

        var contractCode = await _contractRepository.InsertAsync(contract);

        var response = new ContractSaveResponse
        {
            ContractCode = contractCode,
            StoreCode = request.StoreCode,
            ContractNo = contractNo,
            StartDate = contract.ConStart,
            EndDate = contract.ConEnd,
            Created = true,
            Saved = true
        };

        return ApiResponse<ContractSaveResponse>.Ok(
            response,
            "계약이 등록되었습니다.");
    }

    /// <summary>
    /// 기존 계약을 수정한다.
    /// 계약번호와 매장 코드는 유지한다.
    /// </summary>
    private async Task<ApiResponse<ContractSaveResponse>> UpdateContractAsync(
        ContractSaveRequest request, int primaryPartnerCode)
    {
        var existing = await _contractRepository.GetByCodeAsync(request.ContractCode!.Value);

        if (existing == null)
        {
            return ApiResponse<ContractSaveResponse>.Fail(
                AuthErrorCode.ContractNotFound,
                "수정할 계약 정보를 찾을 수 없습니다.");
        }

        if (existing.ConStore != request.StoreCode)
        {
            return ApiResponse<ContractSaveResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "계약의 매장 정보가 일치하지 않습니다.");
        }

        // 계약이 속한 파트너사와 현재 매장의 대표 담당 파트너사가 같아야 함
        // 2026-05-15 계약 수정 시에도 매장의 대표 담당 파트너사와 일치하는지 확인한다.
        if (existing.ConPartner != primaryPartnerCode)
        {
            return ApiResponse<ContractSaveResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "계약의 파트너사 정보와 매장의 담당 파트너사가 일치하지 않습니다.");
        }

        var startDate = request.StartDate?.Date ?? existing.ConStart.Date;
        var endDateResult = ResolveEndDate(request.ContractType, startDate, request.EndDate);

        if (!endDateResult.Success)
        {
            return ApiResponse<ContractSaveResponse>.Fail(
                endDateResult.ErrorCode,
                endDateResult.Message);
        }

        existing.ConType = (int)request.ContractType;
        existing.ConPcc = request.PccamCount;
        existing.ConView = request.ViewerCount;
        existing.ConStart = startDate;
        existing.ConEnd = endDateResult.EndDate;

        if (request.Status != null)
        {
            existing.Status = request.Status.Value;
        }

        var affected = await _contractRepository.UpdateAsync(existing);

        if (affected <= 0)
        {
            return ApiResponse<ContractSaveResponse>.Fail(
                AuthErrorCode.ContractNotFound,
                "계약 정보가 수정되지 않았습니다.");
        }

        var response = new ContractSaveResponse
        {
            ContractCode = existing.ConCode,
            StoreCode = existing.ConStore,
            ContractNo = existing.ConNo,
            StartDate = existing.ConStart,
            EndDate = existing.ConEnd,
            Created = false,
            Saved = true
        };

        return ApiResponse<ContractSaveResponse>.Ok(
            response,
            "계약 정보가 수정되었습니다.");
    }

    /// <summary>
    /// 신규 등록 시 계약 시작일을 결정한다.
    /// 테스트형은 서버 기준 오늘로 처리한다.
    /// </summary>
    private static DateTime ResolveStartDateForCreate(ContractType contractType, DateTime? requestedStartDate)
    {
        if (contractType == ContractType.Trial)
        {
            return DateTime.Today;
        }

        return requestedStartDate?.Date ?? DateTime.Today;
    }

    /// <summary>
    /// 계약 유형별 종료일을 결정한다.
    /// </summary>
    private static ContractEndDateResult ResolveEndDate(
        ContractType contractType,
        DateTime startDate,
        DateTime? requestedEndDate)
    {
        if (contractType == ContractType.Trial)
        {
            return ContractEndDateResult.Ok(startDate.AddDays(15));
        }

        if (contractType == ContractType.Purchase)
        {
            return ContractEndDateResult.Ok(requestedEndDate?.Date);
        }

        if (contractType == ContractType.Subscription)
        {
            if (requestedEndDate == null)
            {
                return ContractEndDateResult.Fail(
                    AuthErrorCode.ContractExpired,
                    "구독형 계약은 종료일이 필요합니다.");
            }

            if (requestedEndDate.Value.Date < startDate.Date)
            {
                return ContractEndDateResult.Fail(
                    AuthErrorCode.ContractExpired,
                    "계약 종료일은 시작일보다 빠를 수 없습니다.");
            }

            return ContractEndDateResult.Ok(requestedEndDate.Value.Date);
        }

        return ContractEndDateResult.Fail(
            AuthErrorCode.ContractInactive,
            "계약 유형이 올바르지 않습니다.");
    }

    /// <summary>
    /// 로그인 사용자가 해당 매장에 접근 가능한지 확인한다.
    /// 
    /// System / 관리자는 모든 매장에 접근할 수 있고,
    /// 담당자는 본인 소속 파트너사에 연결된 매장만 접근할 수 있다.
    /// </summary>
    private async Task<bool> CanAccessStoreAsync(int storeCode, UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return false;
        }

        var loginUserRole = (UserRole)loginUser.UserRole;

        // System / 관리자는 모든 매장 접근 가능
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

    /// <summary>
    /// 중복되지 않는 계약번호를 생성한다.
    /// </summary>
    private async Task<string> GenerateUniqueContractNoAsync(
    ContractType contractType,
    int partnerCode)
    {
        for (var i = 0; i < 20; i++)
        {
            var contractNo = _codeGenerateService.CreateContractNo(
                contractType,
                partnerCode);

            var exists = await _contractRepository.ExistsContractNoAsync(contractNo);

            if (!exists)
            {
                return contractNo;
            }
        }

        throw new InvalidOperationException("중복되지 않는 계약번호를 생성하지 못했습니다.");
    }

    /// <summary>
    /// 매장과 연결되지 않은 파트너사 기준 계약을 신규 등록한다.
    /// 
    /// 정책:
    /// - 계약의 소유 주체는 partnerCode
    /// - 매장은 연결하지 않는다. (ConStore = null)
    /// - System: 허용
    /// - Admin: ContractManage 권한이 있어야 허용
    /// - PartnerUser: 본인 소속 파트너사의 계약만 등록 가능
    /// </summary>
    public async Task<ApiResponse<ContractSaveResponse>> CreatePartnerContractAsync(
        int partnerCode,
        PartnerContractSaveRequest request,
        UserAccount loginUser)
    {
        if (loginUser == null)
        {
            return ApiResponse<ContractSaveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        if (partnerCode <= 0)
        {
            return ApiResponse<ContractSaveResponse>.Fail(
                AuthErrorCode.InvalidStore,
                "파트너사 코드가 올바르지 않습니다.");
        }

        if (request.PccamCount < 0)
        {
            return ApiResponse<ContractSaveResponse>.Fail(
                AuthErrorCode.ContractSlotExceeded,
                "PC캠 허용 수량은 0보다 작을 수 없습니다.");
        }

        if (request.ViewerCount < 0)
        {
            return ApiResponse<ContractSaveResponse>.Fail(
                AuthErrorCode.ContractSlotExceeded,
                "캠뷰어 허용 수량은 0보다 작을 수 없습니다.");
        }

        var loginUserRole = (UserRole)loginUser.UserRole;

        // System / Admin은 계약 관리 권한 확인
        if (loginUserRole == UserRole.System ||
            loginUserRole == UserRole.Admin)
        {
            var permissionResult = await CheckContractManagePermissionAsync(
                loginUser);

            if (!permissionResult.Success)
            {
                return ApiResponse<ContractSaveResponse>.Fail(
                    permissionResult.ErrorCode,
                    permissionResult.Message);
            }
        }
        // 파트너 담당자는 자신의 파트너사 계약만 등록 가능
        else if (loginUserRole == UserRole.PartnerUser)
        {
            if (!loginUser.PartnerCode.HasValue ||
                loginUser.PartnerCode.Value != partnerCode)
            {
                return ApiResponse<ContractSaveResponse>.Fail(
                    AuthErrorCode.PermissionDenied,
                    "본인 소속 파트너사의 계약만 등록할 수 있습니다.");
            }
        }
        else
        {
            return ApiResponse<ContractSaveResponse>.Fail(
                AuthErrorCode.PermissionDenied,
                "계약을 등록할 권한이 없습니다.");
        }

        var startDate = ResolveStartDateForCreate(
            request.ContractType,
            request.StartDate);

        var endDateResult = ResolveEndDate(
            request.ContractType,
            startDate,
            request.EndDate);

        if (!endDateResult.Success)
        {
            return ApiResponse<ContractSaveResponse>.Fail(
                endDateResult.ErrorCode,
                endDateResult.Message);
        }

        var contractNo = await GenerateUniqueContractNoAsync(
            request.ContractType,
            partnerCode);

        var contract = new Contract
        {
            ConStore = null,
            ConPartner = partnerCode,
            ConNo = contractNo,
            ConType = (int)request.ContractType,
            ConPcc = request.PccamCount,
            ConView = request.ViewerCount,
            ConStart = startDate,
            ConEnd = endDateResult.EndDate,
            Status = request.Status ?? (int)ContractStatus.Active
        };

        var contractCode = await _contractRepository.InsertAsync(contract);

        var response = new ContractSaveResponse
        {
            ContractCode = contractCode,
            StoreCode = null,
            ContractNo = contractNo,
            StartDate = contract.ConStart,
            EndDate = contract.ConEnd,
            Created = true,
            Saved = true
        };

        return ApiResponse<ContractSaveResponse>.Ok(
            response,
            "파트너사 계약이 등록되었습니다.");
    }

    /// <summary>
    /// 계약 종료일 계산 결과 내부 객체.
    /// </summary>
    private class ContractEndDateResult
    {
        public bool Success { get; set; }

        public AuthErrorCode ErrorCode { get; set; }

        public string Message { get; set; } = "";

        public DateTime? EndDate { get; set; }

        public static ContractEndDateResult Ok(DateTime? endDate)
        {
            return new ContractEndDateResult
            {
                Success = true,
                ErrorCode = AuthErrorCode.None,
                EndDate = endDate
            };
        }

        public static ContractEndDateResult Fail(AuthErrorCode errorCode, string message)
        {
            return new ContractEndDateResult
            {
                Success = false,
                ErrorCode = errorCode,
                Message = message
            };
        }
    }

    /// <summary>
    /// 계약 관리 권한을 확인한다.
    /// 
    /// System은 자동 허용되고,
    /// Admin은 ContractManage 권한을 보유해야 한다.
    /// </summary>
    private Task<ApiResponse<bool>> CheckContractManagePermissionAsync(
        UserAccount loginUser)
    {
        return _adminPermissionService.CheckPermissionAsync(
            loginUser,
            AdminPermissionType.ContractManage);
    }
}