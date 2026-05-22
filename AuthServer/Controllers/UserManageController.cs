using Microsoft.AspNetCore.Mvc;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.UserManage;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Services;

namespace poscam.AuthServer.Controllers;

/// <summary>
/// 직원/담당자 계정 관리 Controller.
/// 
/// 이 Controller는 관리자 웹에서 사용하는 담당자 관리 API를 제공한다.
/// 
/// 주요 기능:
/// - 담당자 목록 조회
/// - 담당자 상세 조회
/// - 담당자 신규 등록
/// - 담당자 정보 수정
/// - 담당자 상태 변경 요청 등록
/// - 관리자 승인 처리
/// - 관리자 요청 반려
/// - 관리자 상태 변경
/// - 관리자 비밀번호 초기화
/// - 담당자 로그 조회
/// 
/// 실제 권한 판단은 UserManageService에서 처리한다.
/// Controller는 로그인 사용자 확인과 요청/응답 연결만 담당한다.
/// </summary>
[ApiController]
[Route("api/manage/users")]
public class UserManageController : ControllerBase
{
    private readonly UserManageService _userManageService;
    private readonly AccountService _accountService;

    public UserManageController(UserManageService userManageService, AccountService accountService)
    {
        _userManageService = userManageService;
        _accountService = accountService;
    }

    /// <summary>
    /// 담당자 목록 조회 API.
    /// 
    /// 관리자:
    /// - partnerCode 미지정 시 전체 파트너사 담당자 조회
    /// - partnerCode 지정 시 해당 파트너사 담당자 조회
    /// 
    /// 담당자:
    /// - 본인 파트너사 담당자만 조회 가능
    /// 
    /// Query 예:
    /// /api/manage/users
    /// /api/manage/users?partnerCode=1
    /// /api/manage/users?partnerCode=1&userStatus=1
    /// /api/manage/users?requestStatus=1
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<UserManageListItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<UserManageListItemDto>>>> GetUsers(
        [FromQuery] int? partnerCode,
        [FromQuery] int? userStatus,
        [FromQuery] int? requestStatus)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<List<UserManageListItemDto>>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _userManageService.GetUsersAsync(
            partnerCode,
            userStatus,
            requestStatus,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 담당자 상세 조회 API.
    /// 
    /// userCode는 화면에 표시하지 않지만 내부적으로 상세 조회에 사용한다.
    /// 
    /// 관리자:
    /// - 전체 담당자 상세 조회 가능
    /// 
    /// 담당자:
    /// - 본인 파트너사 담당자만 상세 조회 가능
    /// </summary>
    [HttpGet("{userCode:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserManageDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserManageDetailDto>>> GetDetail(
        int userCode)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<UserManageDetailDto>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _userManageService.GetDetailAsync(
            userCode,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 담당자 신규 등록 API.
    /// 
    /// 관리자:
    /// - 모든 파트너사에 담당자 등록 가능
    /// 
    /// 담당자:
    /// - 본인 파트너사에만 담당자 등록 가능
    /// 
    /// 신규 등록된 담당자는 승인대기 상태로 생성된다.
    /// 비밀번호는 서버에서 해시 처리 후 users.user_password_hash에 저장한다.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserSaveResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserSaveResponse>>> CreateUser(
        [FromBody] UserCreateRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<UserSaveResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _userManageService.CreateUserAsync(
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 담당자 정보 수정 API.
    /// 
    /// 현재 정책:
    /// - System 또는 필요한 관리자 세부 권한을 가진 계정만 처리할 수 있다.
    /// - 담당자는 직접 수정하지 않고 변경 요청만 등록
    /// 
    /// 수정 대상:
    /// - 파트너사 코드
    /// - 담당자명
    /// - 연락처
    /// - 이메일
    /// </summary>
    [HttpPut("{userCode:int}")]
    [ProducesResponseType(typeof(ApiResponse<UserSaveResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserSaveResponse>>> UpdateUser(
    int userCode,
    [FromBody] UserUpdateRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<UserSaveResponse>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        request.UserCode = userCode;

        var result = await _userManageService.UpdateUserAsync(
            userCode,
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 담당자 변경 요청 등록 API.
    /// 
    /// 담당자는 실제 상태 변경을 할 수 없고 요청만 등록한다.
    /// 
    /// 요청 유형:
    /// - 2 = 정보수정
    /// - 3 = 비밀번호초기화
    /// - 4 = 일시중지
    /// - 5 = 정상복구
    /// - 6 = 무효
    /// - 9 = 차단
    /// 
    /// 관리자가 이후 요청을 검토하고 처리한다.
    /// </summary>
    [HttpPost("{userCode:int}/requests")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> CreateUserRequest(
        int userCode,
        [FromBody] UserRequestCreateRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<bool>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        request.UserCode = userCode;

        var result = await _userManageService.CreateUserRequestAsync(
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 담당자 가입 승인 API.
    /// 
    /// - System 또는 필요한 관리자 세부 권한을 가진 계정만 처리할 수 있다.
    /// 승인 시:
    /// - users.user_status = 정상
    /// - users.approved_by = 관리자 user_code
    /// - users.approved_at = 현재일시
    /// - users.user_request_status = 처리완료
    /// - userlog에 승인 처리 로그 기록
    /// </summary>
    [HttpPost("{userCode:int}/approve")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> ApproveUser(
        int userCode,
        [FromBody] UserRequestProcessRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<bool>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _userManageService.ApproveUserAsync(
            userCode,
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 최근 요청 반려 API.
    /// 
    /// System 또는 필요한 관리자 세부 권한을 가진 계정만 처리할 수 있다.
    /// 반려 시 실제 user_status는 변경하지 않고,
    /// users.user_request_status만 반려 상태로 변경한다.
    /// </summary>
    [HttpPost("{userCode:int}/reject")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> RejectLatestRequest(
        int userCode,
        [FromBody] UserRequestProcessRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<bool>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _userManageService.RejectLatestRequestAsync(
            userCode,
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 담당자 상태 변경 API.
    /// 
    /// System 또는 필요한 관리자 세부 권한을 가진 계정만 처리할 수 있다.
    /// 
    /// 변경 가능 상태:
    /// - 1 = 정상
    /// - 2 = 일시중지
    /// - 3 = 무효
    /// - 9 = 차단
    /// 
    /// 상태 변경 시 userlog에 처리 로그를 기록한다.
    /// </summary>
    [HttpPost("{userCode:int}/status")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> ChangeStatus(
        int userCode,
        [FromBody] UserStatusChangeRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<bool>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _userManageService.ChangeUserStatusAsync(
            userCode,
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 담당자 비밀번호 초기화 API.
    /// 
    /// System 또는 필요한 관리자 세부 권한을 가진 계정만 처리할 수 있다.
    /// 새 비밀번호는 서버에서 해시 처리 후 users.user_password_hash에 저장한다.
    /// </summary>
    [HttpPost("{userCode:int}/password-reset")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> ResetPassword(
        int userCode,
        [FromBody] UserPasswordChangeRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<bool>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _userManageService.ResetPasswordAsync(
            userCode,
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    /// <summary>
    /// 담당자 로그 조회 API.
    /// 
    /// Systemd / 관리자:
    /// - 전체 담당자 로그 조회 가능
    /// 
    /// 담당자:
    /// - 본인 파트너사 담당자 로그만 조회 가능
    /// 
    /// userlog 테이블의 해당 사용자 이력을 조회한다.
    /// </summary>
    [HttpGet("{userCode:int}/logs")]
    [ProducesResponseType(typeof(ApiResponse<List<UserLogItemDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<UserLogItemDto>>>> GetUserLogs(
        int userCode)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<List<UserLogItemDto>>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _userManageService.GetUserLogsAsync(
            userCode,
            loginUserResult.Data);

        return Ok(result);
    }
    /// <summary>
    /// 담당자 본인 비밀번호 변경.
    /// 
    /// 로그인한 사용자가 본인 계정의 비밀번호만 변경할 수 있습니다.
    /// 현재 비밀번호가 일치해야 새 비밀번호로 변경됩니다.
    /// </summary>
    [HttpPost("{userCode:int}/password-change")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> ChangeMyPassword(
        int userCode,
        [FromBody] UserPasswordSelfChangeRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<bool>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _userManageService.ChangeMyPasswordAsync(
            userCode,
            request,
            loginUserResult.Data);

        return Ok(result);
    }
    /*
     * 중요:
     * 
     * 아래 GetLoginUserAsync()는 기존 StoreManageController, ContractManageController 등에서
     * 이미 사용 중인 로그인 사용자 추출 메서드와 동일한 방식으로 맞춰야 한다.
     * 
     * 만약 현재 프로젝트에서 ControllerBase 공통 클래스를 사용하고 있다면,
     * 이 메서드는 여기서 구현하지 말고 공통 BaseController로 옮기는 것이 좋다.
     * 
     * 현재 프로젝트에 이미 GetLoginUserAsync()가 있다면
     * 이 Controller에서도 같은 방식으로 사용하면 된다.
     */
    /// <summary>
    /// Authorization 헤더의 Bearer 토큰으로 로그인 사용자를 확인한다.
    /// </summary>
    private async Task<ApiResponse<UserAccount>> GetLoginUserAsync()
    {
        var authorizationHeader = Request.Headers.Authorization.FirstOrDefault();

        return await _accountService.GetLoginUserByTokenAsync(authorizationHeader);
    }
}