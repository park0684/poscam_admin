using poscam.AuthServer.Models.Dtos.Config;
using poscam.AuthServer.Models.Dtos.Viewer;
using poscam.AuthServer.Models.Enums;
using Xunit;

namespace poscam.AuthServer.Tests.Services;

public class MultiNvrConfigContractTests
{
    [Fact]
    public void ViewerConfigResponse_DefaultsToSchema2AndSupportsNvrList()
    {
        var response = new ViewerConfigResponse();

        Assert.Equal(2, response.ConfigSchemaVersion);
        Assert.NotNull(response.Nvrs);
        Assert.Empty(response.Nvrs);
        Assert.NotNull(response.Channels);
        Assert.Empty(response.Channels);
    }

    [Fact]
    public void ConfigSyncRequest_KeepsLegacyAndSchema2NvrShapesDuringTransition()
    {
        var request = new ConfigSyncRequest
        {
            ConfigSchemaVersion = 2,
            Nvrs = new List<NvrConfigDto>
            {
                new()
                {
                    NvrNo = 1,
                    NvrProvider = NvrProviderType.Dahua,
                    NvrIp = "192.168.0.101",
                    NvrPort = 37777,
                    NvrRtspPort = 554,
                    NvrChannels = 32
                },
                new()
                {
                    NvrNo = 2,
                    NvrProvider = NvrProviderType.Dahua,
                    NvrIp = "192.168.0.102",
                    NvrPort = 37777,
                    NvrRtspPort = 554,
                    NvrChannels = 16
                }
            },
            NvrConfig = new NvrConfigDto
            {
                NvrNo = 1
            }
        };

        Assert.Equal(2, request.ConfigSchemaVersion);
        Assert.Equal(2, request.Nvrs.Count);
        Assert.Equal(new[] { 1, 2 }, request.Nvrs.Select(x => x.NvrNo).ToArray());
        Assert.NotNull(request.NvrConfig);
    }

    [Fact]
    public void ChannelConfigDto_CarriesNvrNumberIndependentOfChannelNumber()
    {
        var left = new ChannelConfigDto
        {
            PosNo = 1,
            NvrNo = 1,
            ChannelNo = 3,
            Screen = 0
        };

        var right = new ChannelConfigDto
        {
            PosNo = 1,
            NvrNo = 2,
            ChannelNo = 3,
            Screen = 1
        };

        Assert.Equal(left.ChannelNo, right.ChannelNo);
        Assert.NotEqual(left.NvrNo, right.NvrNo);
    }

    [Fact]
    public void MissingNvrNumber_DefaultsToZeroSoSchema2ValidationCanRejectIt()
    {
        var nvr = new NvrConfigDto();
        var channel = new ChannelConfigDto();

        Assert.Equal(0, nvr.NvrNo);
        Assert.Equal(0, channel.NvrNo);
    }

    [Fact]
    public void LatestRequest_CarriesClientConfigSchemaVersion()
    {
        var request = new ConfigLatestRequest
        {
            ConfigSchemaVersion = 2
        };

        Assert.Equal(2, request.ConfigSchemaVersion);
    }

    [Fact]
    public void MultiNvrLegacyBlock_HasDedicatedErrorCode()
    {
        Assert.Equal(6004, (int)AuthErrorCode.ConfigSchemaNotSupported);
    }
}
