using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Dtos.Viewer;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Services;
using Xunit;

namespace poscam.AuthServer.Tests.Services;

public class LegacyConfigSyncPolicyTests
{
    [Fact]
    public void CanContinue_AllowsExistingNvr1OnlyConfig()
    {
        var response = ApiResponse<ViewerConfigResponse>.Ok(
            CreateConfig(1, 1));

        Assert.True(LegacyConfigSyncPolicy.CanContinue(response));
    }

    [Fact]
    public void IsLegacyRepresentable_BlocksSingleNvr2Config()
    {
        var config = CreateConfig(2, 2);

        Assert.False(LegacyConfigSyncPolicy.IsLegacyRepresentable(config));
    }

    [Fact]
    public void IsLegacyRepresentable_BlocksNvr1WithNonLegacyChannelReference()
    {
        var config = CreateConfig(1, 2);

        Assert.False(LegacyConfigSyncPolicy.IsLegacyRepresentable(config));
    }

    [Fact]
    public void IsLegacyRepresentable_BlocksMultipleNvrs()
    {
        var config = CreateConfig(1, 1);
        config.Nvrs.Add(new NvrConfigDto { NvrNo = 2 });

        Assert.False(LegacyConfigSyncPolicy.IsLegacyRepresentable(config));
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
    [InlineData(AuthErrorCode.TokenInvalid)]
    public void CanContinue_BlocksFailuresThatMustNotReachWritePath(
        AuthErrorCode errorCode)
    {
        var response = ApiResponse<ViewerConfigResponse>.Fail(
            errorCode,
            "blocked");

        Assert.False(LegacyConfigSyncPolicy.CanContinue(response));
    }

    [Fact]
    public void CanContinue_BlocksSuccessfulButNonLegacyRepresentableConfig()
    {
        var response = ApiResponse<ViewerConfigResponse>.Ok(
            CreateConfig(2, 2));

        Assert.False(LegacyConfigSyncPolicy.CanContinue(response));
    }

    [Fact]
    public void CanContinue_BlocksMissingPrecheckResult()
    {
        Assert.False(LegacyConfigSyncPolicy.CanContinue(null));
    }

    private static ViewerConfigResponse CreateConfig(
        int nvrNo,
        int channelNvrNo)
    {
        return new ViewerConfigResponse
        {
            Nvrs = new List<NvrConfigDto>
            {
                new NvrConfigDto
                {
                    NvrNo = nvrNo
                }
            },
            Channels = new List<ChannelConfigDto>
            {
                new ChannelConfigDto
                {
                    PosNo = 1,
                    NvrNo = channelNvrNo,
                    ChannelNo = 3,
                    Screen = 0
                }
            }
        };
    }
}
