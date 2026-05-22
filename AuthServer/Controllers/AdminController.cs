using Microsoft.AspNetCore.Mvc;
using poscam.AuthServer.Models.Dtos.Admin;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Services;

namespace poscam.AuthServer.Controllers;

/// <summary>
/// 관리자 API Controller.
/// 
/// 매장 등록, 계약 등록, PC캠 라이선스 발급,
/// NVR 설정 저장, 채널 설정 저장 기능을 제공한다.
/// 
/// Controller는 요청/응답만 담당하고,
/// 실제 업무 판단은 AdminService에서 처리한다.
/// </summary>
//[ApiController]
//[Route("api/admin")]
//public class AdminController : ControllerBase
//{
//    private readonly AdminService _adminService;

//    public AdminController(AdminService adminService)
//    {
//        _adminService = adminService;
//    }

//    /// <summary>
//    /// 신규 매장 등록 API.
//    /// 
//    /// 매장 ID는 백엔드에서 자동 생성한다.
//    /// 최초 비밀번호는 매장 ID와 동일하게 생성된다.
//    /// </summary>
//    /// <param name="request">매장 등록 요청 정보</param>
//    /// <returns>생성된 매장 코드, 매장 ID, 초기 비밀번호</returns>
//    [HttpPost("stores")]
//    [ProducesResponseType(typeof(ApiResponse<StoreCreateResponse>), StatusCodes.Status200OK)]
//    public async Task<ActionResult<ApiResponse<StoreCreateResponse>>> CreateStore(
//        [FromBody] StoreCreateRequest request)
//    {
//        var result = await _adminService.CreateStoreAsync(request);
//        return Ok(result);
//    }

//    /// <summary>
//    /// 신규 계약 등록 API.
//    /// 
//    /// 계약번호는 백엔드에서 자동 생성한다.
//    /// 테스트형 계약은 등록일 기준 15일로 자동 설정된다.
//    /// </summary>
//    /// <param name="request">계약 등록 요청 정보</param>
//    /// <returns>생성된 계약 코드와 계약번호</returns>
//    [HttpPost("contracts")]
//    [ProducesResponseType(typeof(ApiResponse<ContractCreateResponse>), StatusCodes.Status200OK)]
//    public async Task<ActionResult<ApiResponse<ContractCreateResponse>>> CreateContract(
//        [FromBody] ContractCreateRequest request)
//    {
//        var result = await _adminService.CreateContractAsync(request);
//        return Ok(result);
//    }

//    /// <summary>
//    /// PC캠 라이선스 키 발급 API.
//    /// 
//    /// 계약 코드 기준으로 PC캠 인증키를 발급한다.
//    /// 발급 수량은 계약의 PC캠 허용 수량을 초과할 수 없다.
//    /// </summary>
//    /// <param name="request">라이선스 발급 요청 정보</param>
//    /// <returns>발급된 PC캠 라이선스 키 목록</returns>
//    [HttpPost("licenses/issue")]
//    [ProducesResponseType(typeof(ApiResponse<LicenseIssueResponse>), StatusCodes.Status200OK)]
//    public async Task<ActionResult<ApiResponse<LicenseIssueResponse>>> IssuePccamLicenses(
//        [FromBody] LicenseIssueRequest request)
//    {
//        var result = await _adminService.IssuePccamLicensesAsync(request);
//        return Ok(result);
//    }


//    /* 관리자 페이지에서는 NVR/채널 설정을 저장하지 않는다.
//     * NVR/채널 설정은 현장 캠뷰어에서만 수정하고 서버에 동기화한다.
//     * 관리자 페이지는 조회만 가능하다.
//     * 
//     * 만약 매장 담당자에게 NVR/채널 설정 권한을 주려면,
//     * StoreAssignmentRepository에서 해당 매장에 대한 담당자 연결 정보를 조회할 때
//     * AssignmentRole이 Manager인 경우에만 NVR/채널 설정 API 접근 권한을 주도록 하면 된다.
     
//    /// <summary>
//    /// NVR 설정 저장 API.
//    /// 
//    /// 같은 매장에 NVR 설정이 이미 있으면 수정하고,
//    /// 없으면 신규 등록한다.
//    /// </summary>
//    /// <param name="request">NVR 설정 저장 요청 정보</param>
//    /// <returns>저장 결과</returns>
//    [HttpPost("nvr-configs")]
//    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
//    public async Task<ActionResult<ApiResponse<bool>>> SaveNvrConfig(
//        [FromBody] NvrConfigSaveRequest request)
//    {
//        var result = await _adminService.SaveNvrConfigAsync(request);
//        return Ok(result);
//    }

//    /// <summary>
//    /// 채널 매핑 설정 저장 API.
//    /// 
//    /// POS 번호와 NVR 채널 번호를 매핑한다.
//    /// </summary>
//    /// <param name="request">채널 설정 저장 요청 정보</param>
//    /// <returns>저장 결과</returns>
//    [HttpPost("channel-configs")]
//    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
//    public async Task<ActionResult<ApiResponse<bool>>> SaveChannelConfig(
//        [FromBody] ChannelConfigSaveRequest request)
//    {
//        var result = await _adminService.SaveChannelConfigAsync(request);
//        return Ok(result);
//    }
//    */
//}