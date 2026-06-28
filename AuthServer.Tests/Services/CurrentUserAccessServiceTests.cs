using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Repositories;
using poscam.AuthServer.Services;
using Xunit;

namespace poscam.AuthServer.Tests.Services;

public class CurrentUserAccessServiceTests
{
    [Fact]
    public async Task GetCurrentAccessAsync_ForSystem_ReturnsRoleWithoutDatabaseLookup()
    {
        var permissionReader = new FakePermissionReader(new[] { 12 });
        var service = new CurrentUserAccessService(permissionReader);

        var result = await service.GetCurrentAccessAsync(CreateUser(UserRole.System));

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal((int)UserRole.System, result.Data.UserRole);
        Assert.Empty(result.Data.PermissionCodes);
        Assert.Equal(0, permissionReader.CallCount);
    }

    [Fact]
    public async Task GetCurrentAccessAsync_ForAdmin_ReturnsCurrentDatabasePermissions()
    {
        var permissionReader = new FakePermissionReader(new[] { 7, 10, 11, 12 });
        var service = new CurrentUserAccessService(permissionReader);

        var result = await service.GetCurrentAccessAsync(CreateUser(UserRole.Admin));

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(new[] { 7, 10, 11, 12 }, result.Data.PermissionCodes);
        Assert.Equal(15, permissionReader.LastUserCode);
        Assert.Equal(1, permissionReader.CallCount);
    }

    [Fact]
    public async Task GetCurrentAccessAsync_ForPartnerUser_ReturnsEmptyPermissions()
    {
        var permissionReader = new FakePermissionReader(new[] { 12 });
        var service = new CurrentUserAccessService(permissionReader);

        var result = await service.GetCurrentAccessAsync(CreateUser(UserRole.PartnerUser));

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal((int)UserRole.PartnerUser, result.Data.UserRole);
        Assert.Empty(result.Data.PermissionCodes);
        Assert.Equal(0, permissionReader.CallCount);
    }

    [Fact]
    public async Task GetCurrentAccessAsync_WhenPermissionLookupFails_ReturnsDatabaseError()
    {
        var service = new CurrentUserAccessService(new ThrowingPermissionReader());

        var result = await service.GetCurrentAccessAsync(CreateUser(UserRole.Admin));

        Assert.False(result.Success);
        Assert.Equal(AuthErrorCode.DatabaseError, result.ErrorCode);
        Assert.Null(result.Data);
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

    private sealed class FakePermissionReader : IAdminUserPermissionReader
    {
        private readonly List<int> _permissionCodes;

        public FakePermissionReader(IEnumerable<int> permissionCodes)
        {
            _permissionCodes = permissionCodes.ToList();
        }

        public int CallCount { get; private set; }

        public int? LastUserCode { get; private set; }

        public Task<List<int>> GetPermissionCodesAsync(int userCode)
        {
            CallCount++;
            LastUserCode = userCode;
            return Task.FromResult(_permissionCodes.ToList());
        }
    }

    private sealed class ThrowingPermissionReader : IAdminUserPermissionReader
    {
        public Task<List<int>> GetPermissionCodesAsync(int userCode)
        {
            throw new InvalidOperationException("simulated database error");
        }
    }
}
