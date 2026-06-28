using poscam.AuthServer.Options;
using poscam.AuthServer.Services;
using Xunit;

namespace poscam.AuthServer.Tests.Services;

public class FixedTimeSecretComparerTests
{
    private const string ConfiguredKey = "configured-internal-service-key-for-tests";

    [Fact]
    public void MatchesConfiguredSecret_WhenKeysMatch_ReturnsTrue()
    {
        var result = FixedTimeSecretComparer.MatchesConfiguredSecret(
            ConfiguredKey,
            ConfiguredKey,
            AuthPolicyOptions.InternalServiceKeyPlaceholder);

        Assert.True(result);
    }

    [Fact]
    public void MatchesConfiguredSecret_WhenKeyIsWrong_ReturnsFalse()
    {
        var result = FixedTimeSecretComparer.MatchesConfiguredSecret(
            "wrong-key",
            ConfiguredKey,
            AuthPolicyOptions.InternalServiceKeyPlaceholder);

        Assert.False(result);
    }

    [Fact]
    public void MatchesConfiguredSecret_WhenConfigurationIsPlaceholder_ReturnsFalse()
    {
        var result = FixedTimeSecretComparer.MatchesConfiguredSecret(
            AuthPolicyOptions.InternalServiceKeyPlaceholder,
            AuthPolicyOptions.InternalServiceKeyPlaceholder,
            AuthPolicyOptions.InternalServiceKeyPlaceholder);

        Assert.False(result);
    }
}
