using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Options;
using poscam.AuthServer.Services;
using Xunit;

namespace poscam.AuthServer.Tests.Services;

public class AccountTokenServiceTests
{
    private const string NonProductionTestKey = "account-token-unit-test-key-not-for-production";

    [Fact]
    public void ValidateToken_WhenTokenIsValid_ReturnsOriginalPayload()
    {
        var service = CreateService(accountTokenExpireHours: 1);
        var user = CreateUser();

        var token = service.CreateToken(user);
        var result = service.ValidateToken(token);

        Assert.True(result.IsValid);
        Assert.Equal(AuthErrorCode.None, result.ErrorCode);
        Assert.NotNull(result.Payload);
        Assert.Equal(user.UserCode, result.Payload.UserCode);
        Assert.Equal(user.UserRole, result.Payload.UserRole);
    }

    [Fact]
    public void ValidateToken_WhenTokenIsExpired_ReturnsTokenExpired()
    {
        var service = CreateService(accountTokenExpireHours: -1);

        var token = service.CreateToken(CreateUser());
        var result = service.ValidateToken(token);

        Assert.False(result.IsValid);
        Assert.Equal(AuthErrorCode.TokenExpired, result.ErrorCode);
        Assert.Null(result.Payload);
    }

    [Fact]
    public void ValidateToken_WhenTokenIsTampered_ReturnsTokenInvalid()
    {
        var service = CreateService(accountTokenExpireHours: 1);
        var token = service.CreateToken(CreateUser());
        var replacement = token[^1] == 'A' ? 'B' : 'A';
        var tamperedToken = token[..^1] + replacement;

        var result = service.ValidateToken(tamperedToken);

        Assert.False(result.IsValid);
        Assert.Equal(AuthErrorCode.TokenInvalid, result.ErrorCode);
        Assert.Null(result.Payload);
    }

    private static AccountTokenService CreateService(int accountTokenExpireHours)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new AuthPolicyOptions
        {
            TokenSecret = NonProductionTestKey,
            AccountTokenExpireHours = accountTokenExpireHours
        });

        return new AccountTokenService(options);
    }

    private static UserAccount CreateUser()
    {
        return new UserAccount
        {
            UserCode = 15,
            UserId = "test-admin",
            UserName = "테스트 관리자",
            UserRole = (int)UserRole.System,
            UserStatus = (int)UserStatus.Active
        };
    }
}
