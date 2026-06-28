using poscam.AuthServer.Models.Enums;
using Xunit;

namespace poscam.AuthServer.Tests.Models;

public class AdminPermissionTypeTests
{
    [Fact]
    public void PermissionCodes_PreserveExistingValuesAndAddUpdateManageAsTwelve()
    {
        var expected = new Dictionary<AdminPermissionType, int>
        {
            [AdminPermissionType.AdminAccountManage] = 1,
            [AdminPermissionType.AdminPasswordReset] = 2,
            [AdminPermissionType.AdminPermissionManage] = 3,
            [AdminPermissionType.PartnerManage] = 4,
            [AdminPermissionType.PartnerUserManage] = 5,
            [AdminPermissionType.PartnerPricePolicyManage] = 6,
            [AdminPermissionType.StoreManage] = 7,
            [AdminPermissionType.SettlementManage] = 8,
            [AdminPermissionType.PartnerUserPasswordReset] = 9,
            [AdminPermissionType.ContractManage] = 10,
            [AdminPermissionType.LicenseManage] = 11,
            [AdminPermissionType.UpdateManage] = 12
        };

        var actual = Enum.GetValues<AdminPermissionType>();

        Assert.Equal(expected.Count, actual.Length);

        foreach (var permission in actual)
        {
            Assert.True(expected.TryGetValue(permission, out var expectedCode));
            Assert.Equal(expectedCode, (int)permission);
        }

        Assert.Equal(actual.Length, actual.Select(permission => (int)permission).Distinct().Count());
    }
}
