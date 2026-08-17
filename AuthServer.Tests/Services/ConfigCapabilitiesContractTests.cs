using poscam.AuthServer.Models.Dtos.Config;
using Xunit;

namespace poscam.AuthServer.Tests.Services;

public class ConfigCapabilitiesContractTests
{
    [Fact]
    public void ConfigCapabilities_DefaultsToMultiNvrSchema2()
    {
        var response = new ConfigCapabilitiesResponse();

        Assert.Equal(2, response.MaxConfigSchemaVersion);
        Assert.True(response.SupportsMultiNvr);
    }
}
