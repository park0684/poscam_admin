using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Services;
using Xunit;

namespace poscam.AuthServer.Tests.Services;

public class UpdateManagementAuthorizationHelperTests
{
    [Fact]
    public async Task AuthorizeActorAsync_ForSystem_AllowsWithoutPermissionLookup()
    {
        var permissionLookupCalled = false;

        var result = await UpdateManagementAuthorizationHelper.AuthorizeActorAsync(
            CreateUser(UserRole.System),
            (_, _) =>
            {
                permissionLookupCalled = true;
                return Task.FromResult(ApiResponse<bool>.Ok(true));
            });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal((int)UserRole.System, result.Data.UserRole);
        Assert.False(permissionLookupCalled);
    }

    [Fact]
    public async Task AuthorizeActorAsync_ForAdminWithUpdateManage_AllowsAndReturnsActor()
    {
        AdminPermissionType? requestedPermission = null;

        var result = await UpdateManagementAuthorizationHelper.AuthorizeActorAsync(
            CreateUser(UserRole.Admin),
            (_, permission) =>
            {
                requestedPermission = permission;
                return Task.FromResult(ApiResponse<bool>.Ok(true));
            });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(15, result.Data.UserCode);
        Assert.Equal("운영 관리자", result.Data.UserName);
        Assert.Equal((int)UserRole.Admin, result.Data.UserRole);
        Assert.Equal(AdminPermissionType.UpdateManage, requestedPermission);
    }

    [Fact]
    public async Task AuthorizeActorAsync_ForAdminWithoutUpdateManage_Denies()
    {
        var result = await UpdateManagementAuthorizationHelper.AuthorizeActorAsync(
            CreateUser(UserRole.Admin),
            (_, permission) => Task.FromResult(
                ApiResponse<bool>.Fail(
                    AuthErrorCode.PermissionDenied,
                    $"권한 없음: {(int)permission}")));

        Assert.False(result.Success);
        Assert.Equal(AuthErrorCode.PermissionDenied, result.ErrorCode);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task AuthorizeActorAsync_ForPartnerUser_DeniesWithoutPermissionLookup()
    {
        var permissionLookupCalled = false;

        var result = await UpdateManagementAuthorizationHelper.AuthorizeActorAsync(
            CreateUser(UserRole.PartnerUser),
            (_, _) =>
            {
                permissionLookupCalled = true;
                return Task.FromResult(ApiResponse<bool>.Ok(true));
            });

        Assert.False(result.Success);
        Assert.Equal(AuthErrorCode.PermissionDenied, result.ErrorCode);
        Assert.False(permissionLookupCalled);
    }

    private static UserAccount CreateUser(UserRole role)
    {
        return new UserAccount
        {
            UserCode = 15,
            UserName = "운영 관리자",
            UserRole = (int)role,
            UserStatus = (int)UserStatus.Active
        };
    }
}
