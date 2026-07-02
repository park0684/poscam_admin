using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.Settlement;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;

namespace poscam.AuthServer.Services;

/// <summary>
/// 정산 API의 권한 검증과 파트너사 조회 범위를 담당한다.
///
/// 권한 정책:
/// - System은 모든 정산 기능을 사용할 수 있다.
/// - Admin은 기능별 관리자 세부 권한이 필요하다.
/// - PartnerUser는 본인 파트너사의 조회 기능만 사용할 수 있다.
/// - 단가 정책 변경과 정산 변경 기능은 PartnerUser에게 허용하지 않는다.
///
/// 기존 SettlementService는 정산 계산과 저장 로직을 담당하고,
/// 이 서비스는 외부 API에서 호출하기 전 권한 경계를 구성한다.
/// </summary>
public class SettlementAccessService
{
    private readonly SettlementService _settlementService;
    private readonly AdminPermissionService _adminPermissionService;

    public SettlementAccessService(
        SettlementService settlementService,
        AdminPermissionService adminPermissionService)
    {
        _settlementService = settlementService;
        _adminPermissionService = adminPermissionService;
    }

    public async Task<ApiResponse<List<PartnerPricePolicyDto>>> GetPricePoliciesAsync(
        int? partnerCode,
        UserAccount loginUser)
    {
        var permissionResult = await CheckReadPermissionAsync(
            loginUser,
            AdminPermissionType.PartnerPricePolicyManage);

        if (!permissionResult.Success)
        {
            return ApiResponse<List<PartnerPricePolicyDto>>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        return await _settlementService.GetPricePoliciesAsync(
            partnerCode,
            NormalizeSystemUser(loginUser));
    }

    public async Task<ApiResponse<PartnerPricePolicySaveResponse>> SavePricePolicyAsync(
        PartnerPricePolicySaveRequest request,
        UserAccount loginUser)
    {
        var permissionResult = await _adminPermissionService.CheckPermissionAsync(
            loginUser,
            AdminPermissionType.PartnerPricePolicyManage);

        if (!permissionResult.Success)
        {
            return ApiResponse<PartnerPricePolicySaveResponse>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        return await _settlementService.SavePricePolicyAsync(
            request,
            NormalizeSystemUser(loginUser));
    }

    public async Task<ApiResponse<BillingGenerateResponse>> GenerateBillingAsync(
        BillingGenerateRequest request,
        UserAccount loginUser)
    {
        var permissionResult = await _adminPermissionService.CheckPermissionAsync(
            loginUser,
            AdminPermissionType.SettlementManage);

        if (!permissionResult.Success)
        {
            return ApiResponse<BillingGenerateResponse>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        return await _settlementService.GenerateBillingAsync(
            request,
            NormalizeSystemUser(loginUser));
    }

    public async Task<ApiResponse<List<ContractBillingListItemDto>>> GetContractBillingsAsync(
        int billMonth,
        int? partnerCode,
        int? storeCode,
        int? paymentStatus,
        UserAccount loginUser)
    {
        var permissionResult = await CheckReadPermissionAsync(
            loginUser,
            AdminPermissionType.SettlementManage);

        if (!permissionResult.Success)
        {
            return ApiResponse<List<ContractBillingListItemDto>>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        return await _settlementService.GetContractBillingsAsync(
            billMonth,
            partnerCode,
            storeCode,
            paymentStatus,
            NormalizeSystemUser(loginUser));
    }

    public async Task<ApiResponse<List<PartnerMonthlySettlementDto>>> GetPartnerMonthlySettlementsAsync(
        int billMonth,
        int? partnerCode,
        UserAccount loginUser)
    {
        var permissionResult = await CheckReadPermissionAsync(
            loginUser,
            AdminPermissionType.SettlementManage);

        if (!permissionResult.Success)
        {
            return ApiResponse<List<PartnerMonthlySettlementDto>>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        return await _settlementService.GetPartnerMonthlySettlementsAsync(
            billMonth,
            partnerCode,
            NormalizeSystemUser(loginUser));
    }

    public async Task<ApiResponse<List<BillingPaymentDto>>> GetPaymentsAsync(
        int billMonth,
        int? partnerCode,
        int? payStatus,
        UserAccount loginUser)
    {
        var permissionResult = await CheckReadPermissionAsync(
            loginUser,
            AdminPermissionType.SettlementManage);

        if (!permissionResult.Success)
        {
            return ApiResponse<List<BillingPaymentDto>>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        return await _settlementService.GetPaymentsAsync(
            billMonth,
            partnerCode,
            payStatus,
            NormalizeSystemUser(loginUser));
    }

    public async Task<ApiResponse<BillingPaymentDto>> SavePaymentAsync(
        BillingPaymentSaveRequest request,
        UserAccount loginUser)
    {
        var permissionResult = await _adminPermissionService.CheckPermissionAsync(
            loginUser,
            AdminPermissionType.SettlementManage);

        if (!permissionResult.Success)
        {
            return ApiResponse<BillingPaymentDto>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        return await _settlementService.SavePaymentAsync(
            request,
            NormalizeSystemUser(loginUser));
    }

    public async Task<ApiResponse<ContractChargeStatusChangeResponse>> ConfirmContractChargesAsync(
        ContractChargeConfirmRequest request,
        UserAccount loginUser)
    {
        var permissionResult = await _adminPermissionService.CheckPermissionAsync(
            loginUser,
            AdminPermissionType.SettlementManage);

        if (!permissionResult.Success)
        {
            return ApiResponse<ContractChargeStatusChangeResponse>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        return await _settlementService.ConfirmContractChargesAsync(
            request,
            NormalizeSystemUser(loginUser));
    }

    public async Task<ApiResponse<ContractChargeStatusChangeResponse>> CancelPendingContractChargesAsync(
        ContractChargeConfirmRequest request,
        UserAccount loginUser)
    {
        var permissionResult = await _adminPermissionService.CheckPermissionAsync(
            loginUser,
            AdminPermissionType.SettlementManage);

        if (!permissionResult.Success)
        {
            return ApiResponse<ContractChargeStatusChangeResponse>.Fail(
                permissionResult.ErrorCode,
                permissionResult.Message);
        }

        return await _settlementService.CancelPendingContractChargesAsync(
            request,
            NormalizeSystemUser(loginUser));
    }

    /// <summary>
    /// PartnerUser는 자기 파트너사 범위의 조회를 허용한다.
    /// System과 Admin은 관리자 세부 권한 검증을 거친다.
    /// </summary>
    private async Task<ApiResponse<bool>> CheckReadPermissionAsync(
        UserAccount loginUser,
        AdminPermissionType permission)
    {
        if (loginUser == null)
        {
            return ApiResponse<bool>.Fail(
                AuthErrorCode.InvalidLogin,
                "로그인 정보가 없습니다.");
        }

        if (loginUser.UserRole == (int)UserRole.PartnerUser)
        {
            if (loginUser.PartnerCode == null || loginUser.PartnerCode <= 0)
            {
                return ApiResponse<bool>.Fail(
                    AuthErrorCode.PermissionDenied,
                    "담당자 계정에 소속 파트너 정보가 없습니다.");
            }

            return ApiResponse<bool>.Ok(true);
        }

        return await _adminPermissionService.CheckPermissionAsync(
            loginUser,
            permission);
    }

    /// <summary>
    /// 기존 SettlementService의 관리자 판정은 Admin 역할만 허용한다.
    /// 권한 검증을 통과한 System 요청에 한해서 서비스 호출용 역할을 Admin으로 정규화한다.
    /// 사용자 코드와 파트너 정보는 원본 값을 유지한다.
    /// </summary>
    private static UserAccount NormalizeSystemUser(UserAccount loginUser)
    {
        if (loginUser.UserRole != (int)UserRole.System)
        {
            return loginUser;
        }

        return new UserAccount
        {
            UserCode = loginUser.UserCode,
            PartnerCode = loginUser.PartnerCode,
            UserId = loginUser.UserId,
            UserPasswordHash = loginUser.UserPasswordHash,
            UserName = loginUser.UserName,
            UserCell = loginUser.UserCell,
            UserEmail = loginUser.UserEmail,
            UserRole = (int)UserRole.Admin,
            UserStatus = loginUser.UserStatus,
            ApprovedBy = loginUser.ApprovedBy,
            ApprovedAt = loginUser.ApprovedAt,
            UserRDate = loginUser.UserRDate,
            UserUDate = loginUser.UserUDate,
            UserRequestType = loginUser.UserRequestType,
            UserRequestStatus = loginUser.UserRequestStatus,
            UserRequestReason = loginUser.UserRequestReason,
            UserRequestedBy = loginUser.UserRequestedBy,
            UserRequestedAt = loginUser.UserRequestedAt,
            UserRequestResultMemo = loginUser.UserRequestResultMemo,
            PartnerName = loginUser.PartnerName
        };
    }
}
