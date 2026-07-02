using Microsoft.AspNetCore.Mvc;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.Settlement;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Services;

namespace poscam.AuthServer.Controllers;

/// <summary>
/// 정산 관리 Controller.
///
/// 정산 관련 관리자 API를 제공한다.
///
/// 주요 기능:
/// - 파트너사별 단가 정책 조회/저장
/// - 월별 계약 청구자료 생성
/// - 계약별 청구내역 조회
/// - 파트너사별 월 정산 조회
/// - 파트너사별 월 납부 처리 조회/저장
///
/// 실제 권한 판단은 SettlementAccessService에서 처리한다.
/// Controller는 로그인 사용자 확인과 요청/응답 연결만 담당한다.
/// </summary>
[ApiController]
[Route("api/manage/settlements")]
public class SettlementController : ControllerBase
{
    private readonly SettlementAccessService _settlementService;
    private readonly AccountService _accountService;

    public SettlementController(
        SettlementAccessService settlementService,
        AccountService accountService)
    {
        _settlementService = settlementService;
        _accountService = accountService;
    }

    /// <summary>
    /// 파트너사 단가 정책 목록 조회 API.
    ///
    /// 관리자:
    /// - 전체 또는 특정 파트너사 단가 정책 조회 가능.
    ///
    /// 담당자:
    /// - 본인 파트너사 단가 정책만 조회 가능.
    ///
    /// Query 예:
    /// - GET /api/manage/settlements/price-policies
    /// - GET /api/manage/settlements/price-policies?partnerCode=1
    /// </summary>
    [HttpGet("price-policies")]
    [ProducesResponseType(typeof(ApiResponse<List<PartnerPricePolicyDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<PartnerPricePolicyDto>>>> GetPricePolicies(
        [FromQuery] int? partnerCode)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<List<PartnerPricePolicyDto>>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _settlementService.GetPricePoliciesAsync(
            partnerCode,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 파트너사 단가 정책 저장 API.
    ///
    /// 관리자만 가능하다.
    ///
    /// 신규 등록:
    /// - pppCode = 0
    ///
    /// 수정:
    /// - pppCode = 기존 단가 정책 코드
    ///
    /// 단가 정책은 기간이 겹치면 저장할 수 없다.
    /// </summary>
    [HttpPost("price-policies")]
    [ProducesResponseType(typeof(ApiResponse<PartnerPricePolicySaveResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PartnerPricePolicySaveResponse>>> SavePricePolicy(
        [FromBody] PartnerPricePolicySaveRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<PartnerPricePolicySaveResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _settlementService.SavePricePolicyAsync(
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 월별 계약 청구자료 생성 API.
    ///
    /// 관리자만 가능하다.
    ///
    /// 처리 흐름:
    /// 1. 청구월 기준 유효 계약 조회
    /// 2. 계약별 파트너사 단가 조회
    /// 3. 계약 수량 기준 금액 계산
    /// 4. contract_billing 생성
    /// 5. billing_payment 초기 row 생성
    ///
    /// Body 예:
    /// {
    ///   "billMonth": 202605,
    ///   "partnerCode": null,
    ///   "regeneratePending": false
    /// }
    /// </summary>
    [HttpPost("contract-charges/generate")]
    [ProducesResponseType(typeof(ApiResponse<BillingGenerateResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BillingGenerateResponse>>> GenerateBilling(
        [FromBody] BillingGenerateRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<BillingGenerateResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _settlementService.GenerateBillingAsync(
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 계약별 월 청구내역 조회 API.
    ///
    /// 월별 계약 청구내역 화면에서 사용한다.
    ///
    /// Query 예:
    /// - GET /api/manage/settlements/billings?billMonth=202605
    /// - GET /api/manage/settlements/billings?billMonth=202605&amp;partnerCode=1
    /// - GET /api/manage/settlements/billings?billMonth=202605&amp;paymentStatus=0
    /// </summary>
    [HttpGet("contract-charges")]
    [ProducesResponseType(typeof(ApiResponse<List<ContractBillingListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ContractBillingListItemDto>>>> GetContractBillings(
        [FromQuery] int billMonth,
        [FromQuery] int? partnerCode,
        [FromQuery] int? storeCode,
        [FromQuery] int? paymentStatus)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<List<ContractBillingListItemDto>>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _settlementService.GetContractBillingsAsync(
            billMonth,
            partnerCode,
            storeCode,
            paymentStatus,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 파트너사별 월 정산 합산 조회 API.
    ///
    /// 파트너사별 월 정산 화면에서 사용한다.
    /// contract_billing을 bill_month + partner_code 기준으로 합산한다.
    ///
    /// Query 예:
    /// - GET /api/manage/settlements/partners?billMonth=202605
    /// - GET /api/manage/settlements/partners?billMonth=202605&amp;partnerCode=1
    /// </summary>
    [HttpGet("partners")]
    [ProducesResponseType(typeof(ApiResponse<List<PartnerMonthlySettlementDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<PartnerMonthlySettlementDto>>>> GetPartnerMonthlySettlements(
        [FromQuery] int billMonth,
        [FromQuery] int? partnerCode)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<List<PartnerMonthlySettlementDto>>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _settlementService.GetPartnerMonthlySettlementsAsync(
            billMonth,
            partnerCode,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 파트너사별 월 납부 처리 목록 조회 API.
    ///
    /// 납부 처리 화면에서 사용한다.
    ///
    /// Query 예:
    /// - GET /api/manage/settlements/payments?billMonth=202605
    /// - GET /api/manage/settlements/payments?billMonth=202605&amp;partnerCode=1
    /// - GET /api/manage/settlements/payments?billMonth=202605&amp;payStatus=3
    /// </summary>
    [HttpGet("payments")]
    [ProducesResponseType(typeof(ApiResponse<List<BillingPaymentDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<BillingPaymentDto>>>> GetPayments(
        [FromQuery] int billMonth,
        [FromQuery] int? partnerCode,
        [FromQuery] int? payStatus)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<List<BillingPaymentDto>>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _settlementService.GetPaymentsAsync(
            billMonth,
            partnerCode,
            payStatus,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 파트너사별 월 납부 처리 저장 API.
    ///
    /// 관리자만 가능하다.
    ///
    /// 저장 기준:
    /// - billMonth + partnerCode 단위로 저장한다.
    /// - 이미 있으면 수정하고, 없으면 신규 등록한다.
    /// - 저장 후 contract_billing.payment_status를 동기화한다.
    ///
    /// Body 예:
    /// {
    ///   "payCode": 0,
    ///   "billMonth": 202605,
    ///   "partnerCode": 1,
    ///   "payAmount": 95000,
    ///   "payStatus": 3,
    ///   "payDate": "2026-05-31",
    ///   "payMethod": "계좌이체",
    ///   "memo": "5월 정산 납부 완료"
    /// }
    /// </summary>
    [HttpPost("payments")]
    [ProducesResponseType(typeof(ApiResponse<BillingPaymentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<BillingPaymentDto>>> SavePayment(
        [FromBody] BillingPaymentSaveRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<BillingPaymentDto>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _settlementService.SavePaymentAsync(
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    private async Task<ApiResponse<UserAccount>> GetLoginUserAsync()
    {
        var authorizationHeader = Request.Headers.Authorization.FirstOrDefault();

        return await _accountService.GetLoginUserByTokenAsync(authorizationHeader);
    }

    /// <summary>
    /// 계약 청구자료 확정 API.
    ///
    /// 관리자만 가능하다.
    /// 청구대기 상태의 계약 청구자료를 청구확정 상태로 변경한다.
    /// </summary>
    [HttpPost("contract-charges/confirm")]
    [ProducesResponseType(typeof(ApiResponse<ContractChargeStatusChangeResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ContractChargeStatusChangeResponse>>> ConfirmContractCharges(
        [FromBody] ContractChargeConfirmRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<ContractChargeStatusChangeResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _settlementService.ConfirmContractChargesAsync(
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 청구대기 계약 청구자료 취소 API.
    ///
    /// 관리자만 가능하다.
    /// 청구대기 + 미처리 상태의 계약 청구자료만 취소한다.
    /// </summary>
    [HttpPost("contract-charges/reset-confirmed")]
    [ProducesResponseType(typeof(ApiResponse<ContractChargeStatusChangeResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ContractChargeStatusChangeResponse>>> CancelPendingContractCharges(
        [FromBody] ContractChargeConfirmRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<ContractChargeStatusChangeResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _settlementService.CancelPendingContractChargesAsync(
            request,
            loginUserResult.Data);

        return Ok(result);
    }
}
