using poscam.AuthServer.Models.Dtos.Store;
using Xunit;

namespace poscam.AuthServer.Tests.Services;

public class StoreDetailMultiNvrContractTests
{
    [Fact]
    public void StoreDetailResponse_ExposesMultiNvrList()
    {
        var response = new StoreDetailResponse();

        Assert.NotNull(response.Nvrs);
        Assert.Empty(response.Nvrs);
    }

    [Fact]
    public void StoreDetailResponse_ChannelContractIncludesNvrNo()
    {
        var response = new StoreDetailResponse();
        response.ChannelConfigs.Add(
            new poscam.AuthServer.Models.Dtos.Viewer.ChannelConfigDto
            {
                PosNo = 1,
                NvrNo = 2,
                ChannelNo = 7,
                Screen = 1
            });

        Assert.Single(response.ChannelConfigs);
        Assert.Equal(2, response.ChannelConfigs[0].NvrNo);
    }
}
