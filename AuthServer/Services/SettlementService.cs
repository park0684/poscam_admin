using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.Settlement;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Repositories;

namespace poscam.AuthServer.Services;

/// <summary>
/// 정산 관리 Service.
///
/// 역할:
/// - 파트너사별 단가 정책 관리
/// - 월별 계약 청구자료 생성
/// - 계약별 청구내역 조회
/// - 파트너사별 월 정산 조회
/// - 파트너사별 월 납부 처리
///
/// 정산 정책:
/// - PC캠과 캠뷰어 수량은 계약서 등록 수량 기준으로 계산한다.
/// - 단가는 파트너사별 단가 정책을 적용한다.
/// - 월별 청구자료 생성 시 계약 수량과 단가를 contract_billing에 스냅샷으로 저장한다.
/// - 납부 처리는 파트너사 + 청구월 단위로 처리한다.
/// </summary>
public class SettlementService
{
    private readonly PartnerPricePolicyRepository _pricePolicyRepository;
    private readonly ContractBillingRepository _contractBillingRepository;
    private readonly BillingPaymentRepository _billingPaymentRepository;
    private readonly ContractRepository _contractRepository;
    private readonly PartnerRepository _partnerRepository;

    public SettlementService(
        PartnerPricePolicyRepository pricePolicyRepository,
        ContractBillingRepository contractBillingRepository,
        BillingPaymentRepository billingPaymentRepository,
        ContractRepository contractRepository,
        PartnerRepository partnerRepository)
    {
        _pricePolicyRepository = pricePolicyRepository;
        _contractBillingRepository = contractBillingRepository;
        _billingPaymentRepository = billingPaymentRepository;
        _contractRepository = contractRepository;
        _partnerRepository = partnerRepository;
    }

    /// <summary>
    /// 파트너사 단가 정책 목록을 조회한다.
    ///
    /// 관리자:
    /// - 전체 또는 특정 파트너사 단가 정책 조회 가능.
    ///
    /// 담당자:
    /// - 본인 파트너사 단가 정책만 조회 가능.
    /// </summary>
    public async Task<ApiResponse<List<PartnerPricePolicyDto>>> GetPricePoliciesAsync(
        int? partnerCode,
        UserAccount loginUser)
    {
        var resolvedPartnerCode = ResolvePartnerFilter(partnerCode, loginUser);

        if (!resolvedPartnerCode.Success)
        {
            return ApiResponse<List<PartnerPricePolicyDto>>.Fail(
                resolvedPartnerCode.ErrorCode,
                resolvedPartnerCode.Message);
        }

        var list = await _pricePolicyRepository.GetListAsync(resolvedPartnerCode.PartnerCode);

        return ApiResponse<List<PartnerPricePolicyDto>>.Ok(
            list,
            "파트너사 단가 정책을 조회했습니다.");
    }

    /// <summary>
    /// 파트너사 단가 정책을 저장한다.
    ///
    /// 관리자만 가능하다.
    /// 동일 파트너사의 사용 중인 단가 정책 기간이 겹치면 저장하지 않는다.
    /// </summary>
    public async Task<ApiResponse<PartnerPricePolicySaveResponse>> SavePricePolicyAsync(
        PartnerPricePolicySaveRequest request,
        UserAccount loginUser)
    {
        if (!IsAdmin(loginUser))
        {
            return ApiResponse<PartnerPricePolicySaveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "파트너사 단가 정책 저장은 관리자만 가능합니다.");
        }

        var validation = ValidatePricePolicyRequest(request);

        if (!validation.Success)
        {
            return ApiResponse<PartnerPricePolicySaveResponse>.Fail(
                validation.ErrorCode,
                validation.Message);
        }

        var partner = await _partnerRepository.GetByCodeAsync(request.PartnerCode);

        if (partner == null)
        {
            return ApiResponse<PartnerPricePolicySaveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "파트너사 정보를 찾을 수 없습니다.");
        }

        var hasOverlap = await _pricePolicyRepository.ExistsOverlappedPeriodAsync(
            request.PartnerCode,
            request.StartMonth,
            request.EndMonth,
            request.PppCode > 0 ? request.PppCode : null);

        if (hasOverlap)
        {
            return ApiResponse<PartnerPricePolicySaveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "동일 파트너사에 적용 기간이 겹치는 단가 정책이 있습니다.");
        }

        if (request.PppCode <= 0)
        {
            var newCode = await _pricePolicyRepository.InsertAsync(new PartnerPricePolicy
            {
                PartnerCode = request.PartnerCode,
                PppPccamPrice = request.PccamPrice,
                PppViewerPrice = request.ViewerPrice,
                PppStartMonth = request.StartMonth,
                PppEndMonth = request.EndMonth,
                PppStatus = request.Status,
                PppMemo = request.Memo
            });

            return ApiResponse<PartnerPricePolicySaveResponse>.Ok(
                new PartnerPricePolicySaveResponse
                {
                    PppCode = newCode,
                    PartnerCode = request.PartnerCode,
                    Created = true,
                    Saved = true
                },
                "파트너사 단가 정책이 등록되었습니다.");
        }

        var existing = await _pricePolicyRepository.GetByCodeAsync(request.PppCode);

        if (existing == null)
        {
            return ApiResponse<PartnerPricePolicySaveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "수정할 단가 정책을 찾을 수 없습니다.");
        }

        var affected = await _pricePolicyRepository.UpdateAsync(new PartnerPricePolicy
        {
            PppCode = request.PppCode,
            PartnerCode = request.PartnerCode,
            PppPccamPrice = request.PccamPrice,
            PppViewerPrice = request.ViewerPrice,
            PppStartMonth = request.StartMonth,
            PppEndMonth = request.EndMonth,
            PppStatus = request.Status,
            PppMemo = request.Memo
        });

        if (affected <= 0)
        {
            return ApiResponse<PartnerPricePolicySaveResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "단가 정책이 수정되지 않았습니다.");
        }

        return ApiResponse<PartnerPricePolicySaveResponse>.Ok(
            new PartnerPricePolicySaveResponse
            {
                PppCode = request.PppCode,
                PartnerCode = request.PartnerCode,
                Created = false,
                Saved = true
            },
            "파트너사 단가 정책이 수정되었습니다.");
    }

    /// <summary>
    /// 월별 청구자료를 생성한다.
    ///
    /// 관리자만 가능하다.
    /// 생성 흐름:
    /// 1. 청구월 기준 유효 계약 조회.
    /// 2. 계약의 파트너사 기준 유효 단가 조회.
    /// 3. 계약 수량 × 파트너 단가로 금액 계산.
    /// 4. contract_billing에 계약별 청구자료 저장.
    /// 5. billing_payment에 파트너사별 월 납부 대상 row 생성.
    /// </summary>
    public async Task<ApiResponse<BillingGenerateResponse>> GenerateBillingAsync(
        BillingGenerateRequest request,
        UserAccount loginUser)
    {
        if (!IsAdmin(loginUser))
        {
            return ApiResponse<BillingGenerateResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "월별 청구자료 생성은 관리자만 가능합니다.");
        }

        if (!IsValidBillMonth(request.BillMonth))
        {
            return ApiResponse<BillingGenerateResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "청구월 형식이 올바르지 않습니다. 예: 202605");
        }

        var response = new BillingGenerateResponse
        {
            BillMonth = request.BillMonth
        };

        var existingCount = await _contractBillingRepository.CountByMonthAsync(
            request.BillMonth,
            request.PartnerCode);

        if (existingCount > 0)
        {
            if (!request.RegeneratePending)
            {
                return ApiResponse<BillingGenerateResponse>.Fail(
                    AuthErrorCode.InvalidLogin,
                    "이미 생성된 청구자료가 있습니다. 재생성 옵션을 선택하세요.");
            }

            var lockedCount = await _contractBillingRepository.CountLockedBillingAsync(
                request.BillMonth,
                request.PartnerCode);

            if (lockedCount > 0)
            {
                return ApiResponse<BillingGenerateResponse>.Fail(
                    AuthErrorCode.InvalidLogin,
                    "청구확정 또는 납부 처리된 자료가 있어 재생성할 수 없습니다.");
            }

            var deleted = await _contractBillingRepository.DeletePendingAsync(
                request.BillMonth,
                request.PartnerCode);

            response.Messages.Add($"기존 청구대기 자료 {deleted}건을 삭제했습니다.");
        }

        var contracts = await _contractRepository.GetBillingTargetContractsAsync(
            request.BillMonth,
            request.PartnerCode);

        if (contracts.Count == 0)
        {
            response.Messages.Add("청구 대상 계약이 없습니다.");

            return ApiResponse<BillingGenerateResponse>.Ok(
                response,
                "청구 대상 계약이 없습니다.");
        }

        var billings = new List<ContractBilling>();

        foreach (var contract in contracts)
        {
            var pricePolicy = await _pricePolicyRepository.GetActivePolicyAsync(
                contract.PartnerCode,
                request.BillMonth);

            if (pricePolicy == null)
            {
                response.SkippedCount++;
                response.Messages.Add(
                    $"계약 {contract.ContractNo ?? contract.ContractCode.ToString()} : 파트너사 단가 정책이 없어 제외되었습니다.");
                continue;
            }

            var pccamCount = Math.Max(contract.PccamCount, 0);
            var viewerCount = Math.Max(contract.ViewerCount, 0);

            var pccamAmount = pccamCount * pricePolicy.PppPccamPrice;
            var viewerAmount = viewerCount * pricePolicy.PppViewerPrice;
            var totalAmount = pccamAmount + viewerAmount;

            if (totalAmount <= 0)
            {
                response.SkippedCount++;
                response.Messages.Add(
                    $"계약 {contract.ContractNo ?? contract.ContractCode.ToString()} : 청구금액이 0원이라 제외되었습니다.");
                continue;
            }

            billings.Add(new ContractBilling
            {
                BillMonth = request.BillMonth,
                PartnerCode = contract.PartnerCode,
                StoreCode = contract.StoreCode,
                ContractCode = contract.ContractCode,
                ContractNo = contract.ContractNo,
                BillPccamCount = pccamCount,
                BillViewerCount = viewerCount,
                BillPccamUnitPrice = pricePolicy.PppPccamPrice,
                BillViewerUnitPrice = pricePolicy.PppViewerPrice,
                BillPccamAmount = pccamAmount,
                BillViewerAmount = viewerAmount,
                BillTotalAmount = totalAmount,
                BillStatus = 1,
                PaymentStatus = 0,
                BillMemo = "월별 청구자료 자동 생성"
            });
        }

        var createdCount = await _contractBillingRepository.InsertManyAsync(billings);

        response.CreatedCount = createdCount;
        response.TotalAmount = billings.Sum(x => x.BillTotalAmount);

        var initialPaymentCount = await _billingPaymentRepository.CreateInitialPaymentsFromBillingAsync(
            request.BillMonth,
            request.PartnerCode);

        response.Messages.Add($"파트너사별 납부 대상 {initialPaymentCount}건을 생성했습니다.");

        return ApiResponse<BillingGenerateResponse>.Ok(
            response,
            "월별 청구자료가 생성되었습니다.");
    }

    /// <summary>
    /// 계약별 월 청구내역을 조회한다.
    ///
    /// 관리자:
    /// - 전체 또는 특정 파트너사 조회 가능.
    ///
    /// 담당자:
    /// - 본인 파트너사 청구내역만 조회 가능.
    /// </summary>
    public async Task<ApiResponse<List<ContractBillingListItemDto>>> GetContractBillingsAsync(
        int billMonth,
        int? partnerCode,
        int? storeCode,
        int? paymentStatus,
        UserAccount loginUser)
    {
        if (!IsValidBillMonth(billMonth))
        {
            return ApiResponse<List<ContractBillingListItemDto>>.Fail(
                AuthErrorCode.InvalidLogin,
                "청구월 형식이 올바르지 않습니다.");
        }

        var resolvedPartner = ResolvePartnerFilter(partnerCode, loginUser);

        if (!resolvedPartner.Success)
        {
            return ApiResponse<List<ContractBillingListItemDto>>.Fail(
                resolvedPartner.ErrorCode,
                resolvedPartner.Message);
        }

        var list = await _contractBillingRepository.GetListAsync(
            billMonth,
            resolvedPartner.PartnerCode,
            storeCode,
            paymentStatus);

        return ApiResponse<List<ContractBillingListItemDto>>.Ok(
            list,
            "계약별 청구내역을 조회했습니다.");
    }

    /// <summary>
    /// 파트너사별 월 정산 합산 목록을 조회한다.
    /// </summary>
    public async Task<ApiResponse<List<PartnerMonthlySettlementDto>>> GetPartnerMonthlySettlementsAsync(
        int billMonth,
        int? partnerCode,
        UserAccount loginUser)
    {
        if (!IsValidBillMonth(billMonth))
        {
            return ApiResponse<List<PartnerMonthlySettlementDto>>.Fail(
                AuthErrorCode.InvalidLogin,
                "청구월 형식이 올바르지 않습니다.");
        }

        var resolvedPartner = ResolvePartnerFilter(partnerCode, loginUser);

        if (!resolvedPartner.Success)
        {
            return ApiResponse<List<PartnerMonthlySettlementDto>>.Fail(
                resolvedPartner.ErrorCode,
                resolvedPartner.Message);
        }

        var list = await _contractBillingRepository.GetPartnerMonthlySettlementAsync(
            billMonth,
            resolvedPartner.PartnerCode);

        return ApiResponse<List<PartnerMonthlySettlementDto>>.Ok(
            list,
            "파트너사별 월 정산 내역을 조회했습니다.");
    }

    /// <summary>
    /// 파트너사별 월 납부 처리 목록을 조회한다.
    /// </summary>
    public async Task<ApiResponse<List<BillingPaymentDto>>> GetPaymentsAsync(
        int billMonth,
        int? partnerCode,
        int? payStatus,
        UserAccount loginUser)
    {
        if (!IsValidBillMonth(billMonth))
        {
            return ApiResponse<List<BillingPaymentDto>>.Fail(
                AuthErrorCode.InvalidLogin,
                "청구월 형식이 올바르지 않습니다.");
        }

        var resolvedPartner = ResolvePartnerFilter(partnerCode, loginUser);

        if (!resolvedPartner.Success)
        {
            return ApiResponse<List<BillingPaymentDto>>.Fail(
                resolvedPartner.ErrorCode,
                resolvedPartner.Message);
        }

        var list = await _billingPaymentRepository.GetListAsync(
            billMonth,
            resolvedPartner.PartnerCode,
            payStatus);

        return ApiResponse<List<BillingPaymentDto>>.Ok(
            list,
            "납부 처리 내역을 조회했습니다.");
    }

    /// <summary>
    /// 파트너사별 월 납부 처리를 저장한다.
    ///
    /// 관리자만 가능하다.
    /// 저장 후 contract_billing.payment_status도 파트너사 + 청구월 기준으로 동기화한다.
    /// </summary>
    public async Task<ApiResponse<BillingPaymentDto>> SavePaymentAsync(
        BillingPaymentSaveRequest request,
        UserAccount loginUser)
    {
        if (!IsAdmin(loginUser))
        {
            return ApiResponse<BillingPaymentDto>.Fail(
                AuthErrorCode.InvalidLogin,
                "납부 처리는 관리자만 가능합니다.");
        }

        if (!IsValidBillMonth(request.BillMonth))
        {
            return ApiResponse<BillingPaymentDto>.Fail(
                AuthErrorCode.InvalidLogin,
                "청구월 형식이 올바르지 않습니다.");
        }

        if (request.PartnerCode <= 0)
        {
            return ApiResponse<BillingPaymentDto>.Fail(
                AuthErrorCode.InvalidLogin,
                "파트너사 코드가 올바르지 않습니다.");
        }

        if (request.PayAmount < 0)
        {
            return ApiResponse<BillingPaymentDto>.Fail(
                AuthErrorCode.InvalidLogin,
                "납부금액은 0원 이상이어야 합니다.");
        }

        var billAmount = await _contractBillingRepository.GetTotalAmountByPartnerMonthAsync(request.BillMonth, request.PartnerCode);

        if (billAmount <= 0)
        {
            return ApiResponse<BillingPaymentDto>.Fail(
                AuthErrorCode.InvalidLogin,
                "해당 파트너사의 청구금액이 없습니다.");
        }

        if (request.PayAmount < 0)
        {
            return ApiResponse<BillingPaymentDto>.Fail(
                AuthErrorCode.InvalidLogin,
                "납부금액은 0원 이상이어야 합니다.");
        }

        // 미처리로 되돌리는 경우
        // 납부금액, 납부일, 납부방식을 초기화하고 미납금액을 총 청구금액으로 복구한다.
        if (request.PayStatus == 0)
        {
            request.PayAmount = 0;
            request.PayDate = null;
            request.PayMethod = null;
        }

        var payStatus = ResolvePaymentStatus(
            billAmount,
            request.PayAmount,
            request.PayStatus);

        var remainAmount = ResolveRemainAmount(
            billAmount,
            request.PayAmount,
            payStatus);

        var payment = new BillingPayment
        {
            PayCode = request.PayCode,
            BillMonth = request.BillMonth,
            PartnerCode = request.PartnerCode,
            PayBillAmount = billAmount,
            PayAmount = request.PayAmount,
            PayRemainAmount = remainAmount,
            PayStatus = payStatus,
            PayDate = request.PayDate,
            PayMethod = request.PayMethod,
            PayMemo = request.Memo,
            PayCreatedBy = loginUser.UserCode
        };

        var payCode = await _billingPaymentRepository.UpsertByPartnerMonthAsync(payment);

        await _contractBillingRepository.UpdatePaymentStatusByPartnerMonthAsync(
            request.BillMonth,
            request.PartnerCode,
            payStatus);

        var saved = await _billingPaymentRepository.GetByCodeAsync(payCode);

        if (saved == null)
        {
            return ApiResponse<BillingPaymentDto>.Fail(
                AuthErrorCode.InvalidLogin,
                "납부 처리 저장 후 데이터를 조회하지 못했습니다.");
        }

        return ApiResponse<BillingPaymentDto>.Ok(
            ToPaymentDto(saved),
            "납부 처리가 저장되었습니다.");
    }

    /// <summary>
    /// 계약 청구자료를 청구확정 상태로 변경한다.
    ///
    /// 정책:
    /// - 관리자만 가능하다.
    /// - 청구대기 자료만 확정할 수 있다.
    /// - 납부 처리가 이미 진행된 자료는 확정 대상에서 제외한다.
    /// - 확정 이후에는 재생성 대상에서 제외된다.
    /// </summary>
    public async Task<ApiResponse<ContractChargeStatusChangeResponse>> ConfirmContractChargesAsync(
        ContractChargeConfirmRequest request,
        UserAccount loginUser)
    {
        if (!IsAdmin(loginUser))
        {
            return ApiResponse<ContractChargeStatusChangeResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "계약 청구자료 확정은 관리자만 가능합니다.");
        }

        if (!IsValidBillMonth(request.BillMonth))
        {
            return ApiResponse<ContractChargeStatusChangeResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "청구월 형식이 올바르지 않습니다. 예: 202605");
        }

        var totalCount = await _contractBillingRepository.CountByMonthAsync(
            request.BillMonth,
            request.PartnerCode);

        if (totalCount <= 0)
        {
            return ApiResponse<ContractChargeStatusChangeResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "확정할 계약 청구자료가 없습니다.");
        }

        var cancelableCount = await _contractBillingRepository.CountCancelableChargesAsync(
            request.BillMonth,
            request.PartnerCode);

        if (cancelableCount <= 0)
        {
            return ApiResponse<ContractChargeStatusChangeResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "취소 가능한 계약 청구자료가 없습니다. 이미 납부 처리된 자료는 취소할 수 없습니다.");
        }

        var pendingCount = await _contractBillingRepository.CountByBillStatusAsync(
        request.BillMonth,
        request.PartnerCode,
        1);

        if (pendingCount <= 0)
        {
            return ApiResponse<ContractChargeStatusChangeResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "청구대기 상태의 계약 청구자료가 없습니다.");
        }

        var affected = await _contractBillingRepository.ConfirmPendingChargesAsync(
            request.BillMonth,
            request.PartnerCode,
            string.IsNullOrWhiteSpace(request.Memo) ? "계약 청구자료 확정" : request.Memo);

        return ApiResponse<ContractChargeStatusChangeResponse>.Ok(
            new ContractChargeStatusChangeResponse
            {
                BillMonth = request.BillMonth,
                PartnerCode = request.PartnerCode,
                ChangedCount = affected,
                NewBillStatus = 2,
                Message = $"계약 청구자료 {affected}건이 확정되었습니다."
            },
            "계약 청구자료가 확정되었습니다.");
    }

    /// <summary>
    ///
    /// 정책:
    /// - 관리자만 가능하다.
    /// - 납부 처리 되지 않은 청구대기 + 청구확정 자료만 취소할 수 있다.
    /// </summary>
    public async Task<ApiResponse<ContractChargeStatusChangeResponse>> CancelPendingContractChargesAsync(
    ContractChargeConfirmRequest request,
    UserAccount loginUser)
    {
        if (!IsAdmin(loginUser))
        {
            return ApiResponse<ContractChargeStatusChangeResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "계약 청구자료 취소는 관리자만 가능합니다.");
        }

        if (!IsValidBillMonth(request.BillMonth))
        {
            return ApiResponse<ContractChargeStatusChangeResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "청구월 형식이 올바르지 않습니다. 예: 202605");
        }

        var cancelableCount = await _contractBillingRepository.CountCancelableChargesAsync(
            request.BillMonth,
            request.PartnerCode);

        if (cancelableCount <= 0)
        {
            return ApiResponse<ContractChargeStatusChangeResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "취소 가능한 계약 청구자료가 없습니다. 이미 납부 처리된 자료는 취소할 수 없습니다.");
        }

        var memo = string.IsNullOrWhiteSpace(request.Memo)
            ? "청구 취소"
            : request.Memo;

        var affected = await _contractBillingRepository.CancelPendingChargesAsync(
            request.BillMonth,
            request.PartnerCode,
            memo);

        await _billingPaymentRepository.CancelUnprocessedByMonthAsync(
            request.BillMonth,
            request.PartnerCode,
            memo,
            loginUser.UserCode);

        return ApiResponse<ContractChargeStatusChangeResponse>.Ok(
            new ContractChargeStatusChangeResponse
            {
                BillMonth = request.BillMonth,
                PartnerCode = request.PartnerCode,
                ChangedCount = affected,
                NewBillStatus = 9,
                Message = $"계약 청구자료 {affected}건이 대기상태로 초기화 되었습니다."
            },
            "청구확정이 취소되었습니다.");
    }
    private static BillingPaymentDto ToPaymentDto(BillingPayment payment)
    {
        return new BillingPaymentDto
        {
            PayCode = payment.PayCode,
            BillMonth = payment.BillMonth,
            PartnerCode = payment.PartnerCode,
            PayBillAmount = payment.PayBillAmount,
            PayAmount = payment.PayAmount,
            PayRemainAmount = payment.PayRemainAmount,
            PayStatus = payment.PayStatus,
            PayDate = payment.PayDate,
            PayMethod = payment.PayMethod,
            PayMemo = payment.PayMemo,
            PayCreatedBy = payment.PayCreatedBy,
            PayRdate = payment.PayRdate,
            PayUdate = payment.PayUdate
        };
    }

    private static int ResolvePaymentStatus(
    int billAmount,
    int payAmount,
    int requestedStatus)
    {
        // 사용자가 명시적으로 선택한 상태를 우선 인정해야 하는 상태
        // 0=미처리, 4=보류, 9=취소
        if (requestedStatus is 0 or 4 or 9)
        {
            return requestedStatus;
        }

        // 1=미납을 명시적으로 선택한 경우
        if (requestedStatus == 1)
        {
            return 1;
        }

        // 나머지는 납부금액 기준 자동 계산
        if (payAmount <= 0)
        {
            return 1;
        }

        if (payAmount < billAmount)
        {
            return 2;
        }

        return 3;
    }

    private static int ResolveRemainAmount(
        int billAmount,
        int payAmount,
        int payStatus)
    {
        // 미처리 또는 미납은 전체 금액을 미납으로 본다.
        if (payStatus is 0 or 1)
        {
            return billAmount;
        }

        // 취소는 운영 정책에 따라 전체 미납으로 볼지 0으로 볼지 선택 가능.
        // 여기서는 정산에서 빠지는 상태로 보려면 0,
        // 청구 자체가 살아있으면 billAmount로 볼 수 있다.
        if (payStatus == 9)
        {
            return 0;
        }

        return Math.Max(billAmount - payAmount, 0);
    }

    private static bool IsValidBillMonth(int billMonth)
    {
        var year = billMonth / 100;
        var month = billMonth % 100;

        return year is >= 2000 and <= 2099
               && month is >= 1 and <= 12;
    }

    private static bool IsValidPriceMonth(int month)
    {
        return IsValidBillMonth(month);
    }

    private static ServiceValidationResult ValidatePricePolicyRequest(
        PartnerPricePolicySaveRequest request)
    {
        if (request.PartnerCode <= 0)
        {
            return ServiceValidationResult.Fail(
                AuthErrorCode.InvalidLogin,
                "파트너사 코드가 올바르지 않습니다.");
        }

        if (request.PccamPrice < 0 || request.ViewerPrice < 0)
        {
            return ServiceValidationResult.Fail(
                AuthErrorCode.InvalidLogin,
                "단가는 0원 이상이어야 합니다.");
        }

        if (!IsValidPriceMonth(request.StartMonth))
        {
            return ServiceValidationResult.Fail(
                AuthErrorCode.InvalidLogin,
                "적용 시작월 형식이 올바르지 않습니다. 예: 202605");
        }

        if (request.EndMonth != null && !IsValidPriceMonth(request.EndMonth.Value))
        {
            return ServiceValidationResult.Fail(
                AuthErrorCode.InvalidLogin,
                "적용 종료월 형식이 올바르지 않습니다. 예: 202605");
        }

        if (request.EndMonth != null && request.EndMonth.Value < request.StartMonth)
        {
            return ServiceValidationResult.Fail(
                AuthErrorCode.InvalidLogin,
                "적용 종료월은 시작월보다 빠를 수 없습니다.");
        }

        if (request.Status is not 0 and not 1)
        {
            return ServiceValidationResult.Fail(
                AuthErrorCode.InvalidLogin,
                "단가 정책 상태가 올바르지 않습니다.");
        }

        return ServiceValidationResult.Ok();
    }

    private PartnerFilterResult ResolvePartnerFilter(
        int? requestedPartnerCode,
        UserAccount loginUser)
    {
        if (IsAdmin(loginUser))
        {
            return PartnerFilterResult.Ok(requestedPartnerCode);
        }

        if (IsPartnerUser(loginUser))
        {
            if (loginUser.PartnerCode == null)
            {
                return PartnerFilterResult.Fail(
                    AuthErrorCode.InvalidLogin,
                    "담당자 계정에 파트너사가 지정되어 있지 않습니다.");
            }

            if (requestedPartnerCode != null && requestedPartnerCode.Value != loginUser.PartnerCode.Value)
            {
                return PartnerFilterResult.Fail(
                    AuthErrorCode.InvalidLogin,
                    "본인 파트너사의 정산 정보만 조회할 수 있습니다.");
            }

            return PartnerFilterResult.Ok(loginUser.PartnerCode.Value);
        }

        return PartnerFilterResult.Fail(
            AuthErrorCode.InvalidLogin,
            "정산 정보를 조회할 권한이 없습니다.");
    }

    

    private static bool IsAdmin(UserAccount loginUser)
    {
        return loginUser.UserRole == (int)UserRole.Admin;
    }

    private static bool IsPartnerUser(UserAccount loginUser)
    {
        return loginUser.UserRole == (int)UserRole.PartnerUser;
    }

    private class PartnerFilterResult
    {
        public bool Success { get; set; }

        public AuthErrorCode ErrorCode { get; set; }

        public string Message { get; set; } = "";

        public int? PartnerCode { get; set; }

        public static PartnerFilterResult Ok(int? partnerCode)
        {
            return new PartnerFilterResult
            {
                Success = true,
                ErrorCode = AuthErrorCode.None,
                PartnerCode = partnerCode
            };
        }

        public static PartnerFilterResult Fail(
            AuthErrorCode errorCode,
            string message)
        {
            return new PartnerFilterResult
            {
                Success = false,
                ErrorCode = errorCode,
                Message = message
            };
        }
    }

    private class ServiceValidationResult
    {
        public bool Success { get; set; }

        public AuthErrorCode ErrorCode { get; set; }

        public string Message { get; set; } = "";

        public static ServiceValidationResult Ok()
        {
            return new ServiceValidationResult
            {
                Success = true,
                ErrorCode = AuthErrorCode.None
            };
        }

        public static ServiceValidationResult Fail(
            AuthErrorCode errorCode,
            string message)
        {
            return new ServiceValidationResult
            {
                Success = false,
                ErrorCode = errorCode,
                Message = message
            };
        }

        
    }
}
