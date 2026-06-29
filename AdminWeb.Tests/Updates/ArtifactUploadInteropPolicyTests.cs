using poscam.AdminWeb.Models.Updates;
using Xunit;

namespace poscam.AdminWeb.Tests.Updates;

public class ArtifactUploadInteropPolicyTests
{
    [Fact]
    public void BuildUploadUrl_PublicBaseUrl과_ReleaseCode로_직접업로드주소를_생성한다()
    {
        var url = ArtifactUploadUiPolicy.BuildUploadUrl(
            "https://update.example.com/",
            123);

        Assert.Equal(
            "https://update.example.com/api/v1/admin/releases/123/artifacts",
            url);
        Assert.DoesNotContain("token", url.ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("https://user@update.example.com")]
    [InlineData("https://update.example.com?token=secret")]
    [InlineData("https://update.example.com#fragment")]
    public void BuildUploadUrl_잘못되거나_민감정보를포함한_PublicBaseUrl을_거부한다(
        string baseUrl)
    {
        Assert.Throws<ArgumentException>(() =>
            ArtifactUploadUiPolicy.BuildUploadUrl(baseUrl, 1));
    }

    [Fact]
    public void BuildUploadUrl_유효하지않은_ReleaseCode를_거부한다()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ArtifactUploadUiPolicy.BuildUploadUrl(
                "https://update.example.com",
                0));
    }

    [Fact]
    public void HasActiveTarget_대소문자와무관하게_동일활성Target을_찾는다()
    {
        var artifacts = new List<ReleaseArtifactSummaryResponse>
        {
            new()
            {
                Os = "Windows",
                Architecture = "X64",
                PackageType = "FULL",
                Status = 1
            }
        };

        Assert.True(ArtifactUploadUiPolicy.HasActiveTarget(
            artifacts,
            "windows",
            "x64",
            "full"));
    }

    [Fact]
    public void HasActiveTarget_Disabled와_다른Target은_무시한다()
    {
        var artifacts = new List<ReleaseArtifactSummaryResponse>
        {
            new()
            {
                Os = "windows",
                Architecture = "x64",
                PackageType = "full",
                Status = 9
            },
            new()
            {
                Os = "windows",
                Architecture = "x86",
                PackageType = "full",
                Status = 1
            }
        };

        Assert.False(ArtifactUploadUiPolicy.HasActiveTarget(
            artifacts,
            "windows",
            "x64",
            "full"));
    }

    [Theory]
    [InlineData(401, 0)]
    [InlineData(200, 5001)]
    [InlineData(200, 5003)]
    [InlineData(200, 5004)]
    public void IsUnauthorized_HTTP상태와_토큰오류코드를_판정한다(
        int httpStatus,
        int errorCode)
    {
        Assert.True(ArtifactUploadUiPolicy.IsUnauthorized(new ArtifactUploadResult
        {
            HttpStatus = httpStatus,
            ErrorCode = errorCode
        }));
    }

    [Theory]
    [InlineData(403, 0)]
    [InlineData(200, 7001)]
    public void IsForbidden_HTTP상태와_권한오류코드를_판정한다(
        int httpStatus,
        int errorCode)
    {
        Assert.True(ArtifactUploadUiPolicy.IsForbidden(new ArtifactUploadResult
        {
            HttpStatus = httpStatus,
            ErrorCode = errorCode
        }));
    }

    [Theory]
    [InlineData(true, false, 0, 0, "업로드를 취소했습니다.")]
    [InlineData(false, true, 0, 0, "네트워크 오류")]
    [InlineData(false, false, 409, 0, "동일한 Target")]
    [InlineData(false, false, 413, 0, "최대 크기")]
    [InlineData(false, false, 415, 0, "유효한 ZIP")]
    [InlineData(false, false, 500, 0, "서버 오류")]
    [InlineData(false, false, 503, 9003, "권한 확인 서비스")]
    public void GetFailureMessage_업로드실패종류를_구분한다(
        bool cancelled,
        bool networkError,
        int httpStatus,
        int errorCode,
        string expectedText)
    {
        var message = ArtifactUploadUiPolicy.GetFailureMessage(
            new ArtifactUploadResult
            {
                Cancelled = cancelled,
                NetworkError = networkError,
                HttpStatus = httpStatus,
                ErrorCode = errorCode
            });

        Assert.Contains(expectedText, message);
    }
}
