using poscam.AuthServer.Models.Enums;
using Xunit;

namespace poscam.AuthServer.Tests.Models;

public class PartnerUserPermissionTypeTests
{
    [Fact]
    public void PermissionCodes_UseSharedOperationalValues()
    {
        var expected = new Dictionary<PartnerUserPermissionType, int>
        {
            [PartnerUserPermissionType.PartnerUserManage] = 5,
            [PartnerUserPermissionType.StoreManage] = 7,
            [PartnerUserPermissionType.ContractManage] = 10,
            [PartnerUserPermissionType.LicenseManage] = 11,
            [PartnerUserPermissionType.DeviceManage] = 13
        };

        var actual = Enum.GetValues<PartnerUserPermissionType>();

        Assert.Equal(expected.Count, actual.Length);

        foreach (var permission in actual)
        {
            Assert.True(expected.TryGetValue(permission, out var expectedCode));
            Assert.Equal(expectedCode, (int)permission);
        }
    }
}
