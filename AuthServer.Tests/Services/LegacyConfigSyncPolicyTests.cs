using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.Viewer;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Services;
using Xunit;

namespace poscam.AuthServer.Tests.Services;

public class LegacyConfigSyncPolicyTests
{
    [Fact]
    public void CanContinue_AllowsExistingSingleNvrConfig()
    {
        var response = ApiResponse<ViewerConfigResponse>.Ok(
            new ViewerConfigResponse());

        Assert.True(LegacyConfigSyncPolicy.CanContinue(response));
    }

    [Fact]
    public void CanContinue_AllowsFirstUploadWhenNoNvrConfigExists()
    {
        var response = ApiResponse<ViewerConfigResponse>.Fail(
            AuthErrorCode.NvrConfigNotFound,
            "NVR 설정을 찾을 수 없습니다.");

        Assert.True(LegacyConfigSyncPolicy.CanContinue(response));
    }

    [Theory]
    [InlineData(AuthErrorCode.ConfigSchemaNotSupported)]
    [InlineData(AuthErrorCode.ConfigVersionConflict)]
    [InlineData(AuthErrorCode.InvalidToken)]
    public void CanContinue_BlocksFailuresThatMustNotReachWritePath(
        AuthErrorCode errorCode)
    {
        var response = ApiResponse<ViewerConfigResponse>.Fail(
            errorCode,
            "blocked");

        Assert.False(LegacyConfigSyncPolicy.CanContinue(response));
    }

    [Fact]
    public void CanContinue_BlocksMissingPrecheckResult()
    {
        Assert.False(LegacyConfigSyncPolicy.CanContinue(null));
    }
}
