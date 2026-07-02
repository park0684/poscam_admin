using Microsoft.AspNetCore.Mvc;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.UserManage;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Services;

namespace poscam.AuthServer.Controllers;

[ApiController]
[Route("api/manage/users")]
public class PartnerUserPermissionController : ControllerBase
{
    private readonly PartnerUserPermissionManageService _permissionService;
    private readonly AccountService _accountService;

    public PartnerUserPermissionController(
        PartnerUserPermissionManageService permissionService,
        AccountService accountService)
    {
        _permissionService = permissionService;
        _accountService = accountService;
    }

    [HttpGet("{userCode:int}/permissions")]
    [ProducesResponseType(typeof(ApiResponse<List<int>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<int>>>> GetPermissions(
        int userCode)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<List<int>>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        var result = await _permissionService.GetPermissionsAsync(
            userCode,
            loginUserResult.Data);

        return Ok(result);
    }

    [HttpPut("{userCode:int}/permissions")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> UpdatePermissions(
        int userCode,
        [FromBody] PartnerUserPermissionUpdateRequest request)
    {
        var loginUserResult = await GetLoginUserAsync();

        if (!loginUserResult.Success || loginUserResult.Data == null)
        {
            return Ok(ApiResponse<bool>.Fail(
                loginUserResult.ErrorCode,
                loginUserResult.Message));
        }

        request.UserCode = userCode;

        var result = await _permissionService.UpdatePermissionsAsync(
            userCode,
            request,
            loginUserResult.Data);

        return Ok(result);
    }

    private async Task<ApiResponse<UserAccount>> GetLoginUserAsync()
    {
        var authorizationHeader = Request.Headers.Authorization.FirstOrDefault();

        return await _accountService.GetLoginUserByTokenAsync(
            authorizationHeader);
    }
}
