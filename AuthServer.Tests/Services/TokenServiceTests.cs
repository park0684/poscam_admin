using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Options;
using poscam.AuthServer.Services;
using Xunit;

namespace poscam.AuthServer.Tests.Services;

public class TokenServiceTests
{
    private const string NonProductionTestKey =
        "viewer-token-unit-test-key-not-for-production";

    [Fact]
    public void ValidateToken_WhenAccessTokenExpired_ReturnsTokenExpired()
    {
        var service = CreateService(
            tokenExpireHours: -1,
            viewerOfflineDays: 7);
        var token = CreateViewerToken(service).Token;

        var result = service.ValidateToken(token);

        Assert.False(result.IsValid);
        Assert.Equal(AuthErrorCode.TokenExpired, result.ErrorCode);
        Assert.Null(result.Payload);
    }

    [Fact]
    public void ValidateTokenForRenewal_WhenExpiredWithinOfflineWindow_ReturnsPayload()
    {
        var service = CreateService(
            tokenExpireHours: -1,
            viewerOfflineDays: 7);
        var token = CreateViewerToken(service).Token;

        var result = service.ValidateTokenForRenewal(token);

        Assert.True(result.IsValid);
        Assert.Equal(AuthErrorCode.None, result.ErrorCode);
        Assert.NotNull(result.Payload);
        Assert.Equal((int)DeviceAppType.Viewer, result.Payload.AppType);
        Assert.Equal(301, result.Payload.DeviceCode);
        Assert.Equal("VIEWER-HWID", result.Payload.Hwid);
    }

    [Fact]
    public void ValidateTokenForRenewal_WhenOfflineWindowExpired_ReturnsOfflineExpired()
    {
        var service = CreateService(
            tokenExpireHours: -48,
            viewerOfflineDays: -1,
            subscriptionOfflineDays: -1);
        var token = CreateViewerToken(service).Token;

        var result = service.ValidateTokenForRenewal(token);

        Assert.False(result.IsValid);
        Assert.Equal(AuthErrorCode.OfflineExpired, result.ErrorCode);
        Assert.Null(result.Payload);
    }

    [Fact]
    public void ValidateTokenForRenewal_WhenTokenIsTampered_ReturnsTokenInvalid()
    {
        var service = CreateService(
            tokenExpireHours: -1,
            viewerOfflineDays: 7);
        var token = CreateViewerToken(service).Token;
        var replacement = token[^1] == 'A' ? 'B' : 'A';
        var tamperedToken = token[..^1] + replacement;

        var result = service.ValidateTokenForRenewal(tamperedToken);

        Assert.False(result.IsValid);
        Assert.Equal(AuthErrorCode.TokenInvalid, result.ErrorCode);
        Assert.Null(result.Payload);
    }

    [Theory]
    [InlineData(ContractType.Trial)]
    [InlineData(ContractType.Subscription)]
    [InlineData(ContractType.Purchase)]
    public void CreateToken_WhenAppTypeIsViewer_UsesSevenDayViewerPolicy(
        ContractType contractType)
    {
        var service = CreateService(
            tokenExpireHours: 24,
            viewerOfflineDays: 7,
            trialOfflineDays: 1,
            subscriptionOfflineDays: 3,
            purchaseOfflineDays: 3650);

        var token = CreateViewerToken(service, contractType);
        var offlineDays =
            (token.OfflineUntil - token.IssuedAt).TotalDays;

        Assert.InRange(offlineDays, 6.999, 7.001);
    }

    [Fact]
    public void CreateToken_WhenAppTypeIsPccam_KeepsPccamOfflinePolicy()
    {
        var service = CreateService(
            tokenExpireHours: 24,
            viewerOfflineDays: 7,
            pccamOfflineDays: 5);

        var token = service.CreateToken(
            storeCode: 101,
            contractCode: 201,
            licenseCode: 401,
            deviceCode: 302,
            appType: DeviceAppType.Pccam,
            hwid: "PCCAM-HWID",
            contractType: ContractType.Subscription,
            isPermanent: false);
        var offlineDays =
            (token.OfflineUntil - token.IssuedAt).TotalDays;

        Assert.InRange(offlineDays, 4.999, 5.001);
    }

    private static TokenService CreateService(
        int tokenExpireHours,
        int viewerOfflineDays,
        int pccamOfflineDays = 7,
        int trialOfflineDays = 1,
        int subscriptionOfflineDays = 3,
        int purchaseOfflineDays = 3650)
    {
        var options = Microsoft.Extensions.Options.Options.Create(
            new AuthPolicyOptions
            {
                TokenSecret = NonProductionTestKey,
                TokenExpireHours = tokenExpireHours,
                PccamOfflineDays = pccamOfflineDays,
                ViewerOfflineDays = viewerOfflineDays,
                TrialOfflineDays = trialOfflineDays,
                SubscriptionOfflineDays = subscriptionOfflineDays,
                PurchaseOfflineDays = purchaseOfflineDays
            });

        return new TokenService(options);
    }

    private static poscam.AuthServer.Models.Dtos.Common.AuthTokenDto CreateViewerToken(
        TokenService service,
        ContractType contractType = ContractType.Subscription)
    {
        return service.CreateToken(
            storeCode: 101,
            contractCode: 201,
            licenseCode: null,
            deviceCode: 301,
            appType: DeviceAppType.Viewer,
            hwid: "VIEWER-HWID",
            contractType: contractType,
            isPermanent: false,
            configVersion: "7");
    }
}
