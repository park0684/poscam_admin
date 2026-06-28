using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using poscam.AuthServer.Controllers;
using poscam.AuthServer.Models.Dtos.Account;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Options;
using Xunit;

namespace poscam.AuthServer.Tests.Services;

public class InternalUpdateManagementControllerTests
{
    private const string ConfiguredServiceKey = "configured-service-key-for-controller-tests";

    [Fact]
    public async Task Authorize_ForSystem_ReturnsActorWithoutPermissionLookup()
    {
        var permissionLookupCalled = false;
        var controller = CreateController(
            _ => Task.FromResult(LoginSuccess(UserRole.System)),
            (_, _) =>
            {
                permissionLookupCalled = true;
                return Task.FromResult(ApiResponse<bool>.Ok(true));
            });

        var result = await controller.Authorize(ConfiguredServiceKey);

        var response = AssertOk(result);
        Assert.Equal((int)UserRole.System, response.Data!.UserRole);
        Assert.False(permissionLookupCalled);
    }

    [Fact]
    public async Task Authorize_ForAdminWithUpdateManage_ReturnsActor()
    {
        AdminPermissionType? requestedPermission = null;
        var controller = CreateController(
            _ => Task.FromResult(LoginSuccess(UserRole.Admin)),
            (_, permission) =>
            {
                requestedPermission = permission;
                return Task.FromResult(ApiResponse<bool>.Ok(true));
            });

        var result = await controller.Authorize(ConfiguredServiceKey);

        var response = AssertOk(result);
        Assert.Equal((int)UserRole.Admin, response.Data!.UserRole);
        Assert.Equal(AdminPermissionType.UpdateManage, requestedPermission);
    }

    [Fact]
    public async Task Authorize_ForAdminWithoutUpdateManage_ReturnsForbidden()
    {
        var controller = CreateController(
            _ => Task.FromResult(LoginSuccess(UserRole.Admin)),
            (_, _) => Task.FromResult(ApiResponse<bool>.Fail(
                AuthErrorCode.PermissionDenied,
                "권한이 없습니다.")));

        var result = await controller.Authorize(ConfiguredServiceKey);

        AssertStatusCode(result, StatusCodes.Status403Forbidden, AuthErrorCode.PermissionDenied);
    }

    [Fact]
    public async Task Authorize_ForPartnerUser_ReturnsForbiddenWithoutPermissionLookup()
    {
        var permissionLookupCalled = false;
        var controller = CreateController(
            _ => Task.FromResult(LoginSuccess(UserRole.PartnerUser)),
            (_, _) =>
            {
                permissionLookupCalled = true;
                return Task.FromResult(ApiResponse<bool>.Ok(true));
            });

        var result = await controller.Authorize(ConfiguredServiceKey);

        AssertStatusCode(result, StatusCodes.Status403Forbidden, AuthErrorCode.PermissionDenied);
        Assert.False(permissionLookupCalled);
    }

    [Fact]
    public async Task Authorize_WhenServiceKeyIsWrong_ReturnsUnauthorizedBeforeTokenLookup()
    {
        var tokenLookupCalled = false;
        var controller = CreateController(
            _ =>
            {
                tokenLookupCalled = true;
                return Task.FromResult(LoginSuccess(UserRole.System));
            },
            (_, _) => Task.FromResult(ApiResponse<bool>.Ok(true)));

        var result = await controller.Authorize("wrong-key");

        AssertUnauthorized(result, AuthErrorCode.InvalidLogin);
        Assert.False(tokenLookupCalled);
    }

    [Fact]
    public async Task Authorize_WhenTokenIsInvalid_ReturnsUnauthorized()
    {
        var controller = CreateController(
            _ => Task.FromResult(ApiResponse<UserAccount>.Fail(
                AuthErrorCode.TokenInvalid,
                "토큰이 유효하지 않습니다.")),
            (_, _) => Task.FromResult(ApiResponse<bool>.Ok(true)));

        var result = await controller.Authorize(ConfiguredServiceKey);

        AssertUnauthorized(result, AuthErrorCode.TokenInvalid);
    }

    [Fact]
    public async Task Authorize_WhenAccountIsInactive_ReturnsUnauthorized()
    {
        var controller = CreateController(
            _ => Task.FromResult(ApiResponse<UserAccount>.Fail(
                AuthErrorCode.InvalidLogin,
                "현재 사용할 수 없는 계정입니다.")),
            (_, _) => Task.FromResult(ApiResponse<bool>.Ok(true)));

        var result = await controller.Authorize(ConfiguredServiceKey);

        AssertUnauthorized(result, AuthErrorCode.InvalidLogin);
    }

    [Fact]
    public async Task Authorize_WhenAccountLookupThrows_ReturnsDatabaseError()
    {
        var controller = CreateController(
            _ => throw new InvalidOperationException("simulated database error"),
            (_, _) => Task.FromResult(ApiResponse<bool>.Ok(true)));

        var result = await controller.Authorize(ConfiguredServiceKey);

        AssertStatusCode(result, StatusCodes.Status500InternalServerError, AuthErrorCode.DatabaseError);
    }

    private static TestInternalUpdateManagementController CreateController(
        Func<string?, Task<ApiResponse<UserAccount>>> getLoginUserAsync,
        Func<UserAccount, AdminPermissionType, Task<ApiResponse<bool>>> checkPermissionAsync)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new AuthPolicyOptions
        {
            InternalServiceKey = ConfiguredServiceKey
        });

        var controller = new TestInternalUpdateManagementController(
            options,
            getLoginUserAsync,
            checkPermissionAsync)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        controller.Request.Headers.Authorization = "Bearer test-account-token";
        return controller;
    }

    private static ApiResponse<UserAccount> LoginSuccess(UserRole role)
    {
        return ApiResponse<UserAccount>.Ok(new UserAccount
        {
            UserCode = 15,
            UserName = "운영 관리자",
            UserRole = (int)role,
            UserStatus = (int)UserStatus.Active
        });
    }

    private static ApiResponse<UpdateManagementActorResponse> AssertOk(
        ActionResult<ApiResponse<UpdateManagementActorResponse>> result)
    {
        var objectResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<UpdateManagementActorResponse>>(objectResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        return response;
    }

    private static void AssertUnauthorized(
        ActionResult<ApiResponse<UpdateManagementActorResponse>> result,
        AuthErrorCode expectedErrorCode)
    {
        var objectResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<UpdateManagementActorResponse>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal(expectedErrorCode, response.ErrorCode);
    }

    private static void AssertStatusCode(
        ActionResult<ApiResponse<UpdateManagementActorResponse>> result,
        int expectedStatusCode,
        AuthErrorCode expectedErrorCode)
    {
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(expectedStatusCode, objectResult.StatusCode);
        var response = Assert.IsType<ApiResponse<UpdateManagementActorResponse>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal(expectedErrorCode, response.ErrorCode);
    }

    private sealed class TestInternalUpdateManagementController
        : InternalUpdateManagementController
    {
        public TestInternalUpdateManagementController(
            IOptions<AuthPolicyOptions> authPolicyOptions,
            Func<string?, Task<ApiResponse<UserAccount>>> getLoginUserAsync,
            Func<UserAccount, AdminPermissionType, Task<ApiResponse<bool>>> checkPermissionAsync)
            : base(authPolicyOptions, getLoginUserAsync, checkPermissionAsync)
        {
        }
    }
}
