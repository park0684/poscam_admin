using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Repositories;
using poscam.AuthServer.Services;
using Xunit;

namespace poscam.AuthServer.Tests.Services;

public class PartnerUserPermissionServiceTests
{
    [Fact]
    public async Task CheckPermissionAsync_WithPermission_ReturnsSuccess()
    {
        var reader = new FakePermissionReader(hasPermission: true);
        var service = new PartnerUserPermissionService(reader);

        var result = await service.CheckPermissionAsync(
            CreateUser(UserRole.PartnerUser),
            PartnerUserPermissionType.DeviceManage);

        Assert.True(result.Success);
        Assert.Equal(15, reader.LastUserCode);
        Assert.Equal(
            PartnerUserPermissionType.DeviceManage,
            reader.LastPermission);
    }

    [Fact]
    public async Task CheckPermissionAsync_WithoutPermission_ReturnsPermissionDenied()
    {
        var service = new PartnerUserPermissionService(
            new FakePermissionReader(hasPermission: false));

        var result = await service.CheckPermissionAsync(
            CreateUser(UserRole.PartnerUser),
            PartnerUserPermissionType.DeviceManage);

        Assert.False(result.Success);
        Assert.Equal(AuthErrorCode.PermissionDenied, result.ErrorCode);
    }

    [Fact]
    public async Task CheckPermissionAsync_ForAdmin_ReturnsPermissionDeniedWithoutLookup()
    {
        var reader = new FakePermissionReader(hasPermission: true);
        var service = new PartnerUserPermissionService(reader);

        var result = await service.CheckPermissionAsync(
            CreateUser(UserRole.Admin),
            PartnerUserPermissionType.DeviceManage);

        Assert.False(result.Success);
        Assert.Equal(AuthErrorCode.PermissionDenied, result.ErrorCode);
        Assert.Equal(0, reader.CallCount);
    }

    private static UserAccount CreateUser(UserRole role)
    {
        return new UserAccount
        {
            UserCode = 15,
            UserName = "담당자",
            UserRole = (int)role,
            UserStatus = (int)UserStatus.Active
        };
    }

    private sealed class FakePermissionReader : IPartnerUserPermissionReader
    {
        private readonly bool _hasPermission;

        public FakePermissionReader(bool hasPermission)
        {
            _hasPermission = hasPermission;
        }

        public int CallCount { get; private set; }
        public int? LastUserCode { get; private set; }
        public PartnerUserPermissionType? LastPermission { get; private set; }

        public Task<bool> ExistsPermissionAsync(
            int userCode,
            PartnerUserPermissionType permission)
        {
            CallCount++;
            LastUserCode = userCode;
            LastPermission = permission;
            return Task.FromResult(_hasPermission);
        }

        public Task<List<int>> GetPermissionCodesAsync(int userCode)
        {
            return Task.FromResult(new List<int>());
        }
    }
}
