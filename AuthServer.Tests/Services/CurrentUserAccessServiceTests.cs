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
        var adminReader = new FakeAdminPermissionReader(new[] { 12 });
        var partnerReader = new FakePartnerPermissionReader(new[] { 5, 7 });
        var service = new CurrentUserAccessService(adminReader, partnerReader);

        var result = await service.GetCurrentAccessAsync(CreateUser(UserRole.System));

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal((int)UserRole.System, result.Data.UserRole);
        Assert.Empty(result.Data.PermissionCodes);
        Assert.Equal(0, adminReader.CallCount);
        Assert.Equal(0, partnerReader.CallCount);
    }

    [Fact]
    public async Task GetCurrentAccessAsync_ForAdmin_ReturnsCurrentDatabasePermissions()
    {
        var adminReader = new FakeAdminPermissionReader(new[] { 7, 10, 11, 12 });
        var partnerReader = new FakePartnerPermissionReader(Array.Empty<int>());
        var service = new CurrentUserAccessService(adminReader, partnerReader);

        var result = await service.GetCurrentAccessAsync(CreateUser(UserRole.Admin));

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(new[] { 7, 10, 11, 12 }, result.Data.PermissionCodes);
        Assert.Equal(15, adminReader.LastUserCode);
        Assert.Equal(1, adminReader.CallCount);
        Assert.Equal(0, partnerReader.CallCount);
    }

    [Fact]
    public async Task GetCurrentAccessAsync_ForPartnerUser_ReturnsPartnerPermissions()
    {
        var adminReader = new FakeAdminPermissionReader(Array.Empty<int>());
        var partnerReader = new FakePartnerPermissionReader(new[] { 5, 7, 11, 13 });
        var service = new CurrentUserAccessService(adminReader, partnerReader);

        var result = await service.GetCurrentAccessAsync(CreateUser(UserRole.PartnerUser));

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal((int)UserRole.PartnerUser, result.Data.UserRole);
        Assert.Equal(new[] { 5, 7, 11, 13 }, result.Data.PermissionCodes);
        Assert.Equal(15, partnerReader.LastUserCode);
        Assert.Equal(0, adminReader.CallCount);
        Assert.Equal(1, partnerReader.CallCount);
    }

    [Fact]
    public async Task GetCurrentAccessAsync_WhenPermissionLookupFails_ReturnsDatabaseError()
    {
        var service = new CurrentUserAccessService(
            new ThrowingAdminPermissionReader(),
            new FakePartnerPermissionReader(Array.Empty<int>()));

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

    private sealed class FakeAdminPermissionReader : IAdminUserPermissionReader
    {
        private readonly List<int> _permissionCodes;

        public FakeAdminPermissionReader(IEnumerable<int> permissionCodes)
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

    private sealed class FakePartnerPermissionReader : IPartnerUserPermissionReader
    {
        private readonly List<int> _permissionCodes;

        public FakePartnerPermissionReader(IEnumerable<int> permissionCodes)
        {
            _permissionCodes = permissionCodes.ToList();
        }

        public int CallCount { get; private set; }
        public int? LastUserCode { get; private set; }

        public Task<bool> ExistsPermissionAsync(
            int userCode,
            PartnerUserPermissionType permission)
        {
            return Task.FromResult(
                _permissionCodes.Contains((int)permission));
        }

        public Task<List<int>> GetPermissionCodesAsync(int userCode)
        {
            CallCount++;
            LastUserCode = userCode;
            return Task.FromResult(_permissionCodes.ToList());
        }
    }

    private sealed class ThrowingAdminPermissionReader : IAdminUserPermissionReader
    {
        public Task<List<int>> GetPermissionCodesAsync(int userCode)
        {
            throw new InvalidOperationException("simulated database error");
        }
    }
}
