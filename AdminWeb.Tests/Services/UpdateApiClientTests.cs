using System.Net;
using System.Net.Http.Json;
using Microsoft.JSInterop;
using poscam.AdminWeb.Models.Common;
using poscam.AdminWeb.Models.Updates;
using poscam.AdminWeb.Services;
using Xunit;

namespace poscam.AdminWeb.Tests.Services;

public class UpdateApiClientTests
{
    [Fact]
    public async Task GetAsync_요청마다_BearerToken과_InternalBaseUrl을_사용한다()
    {
        var handler = new RecordingHandler(_ => SuccessResponse(
            new ActiveProductResponse
            {
                ProductCode = "PCCAM",
                ProductName = "PC CAM"
            }));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient, "account-token");

        var result = await client.GetAsync<ActiveProductResponse>(
            "api/v1/admin/products/active");

        Assert.True(result.Success);
        Assert.Equal("account-token", handler.AuthorizationToken);
        Assert.Equal(
            "https://update.internal/api/v1/admin/products/active",
            handler.RequestUri?.ToString());
    }

    [Fact]
    public async Task PostAsync_요청Body를_camelCase_JSON으로_전송한다()
    {
        var handler = new RecordingHandler(_ => SuccessResponse(
            new ReleaseDetailResponse
            {
                ReleaseCode = 10,
                ProductCode = "PCCAM",
                Version = "1.2.3",
                Channel = "stable"
            }));
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient, "account-token");

        var result = await client.PostAsync<
            CreateReleaseRequest,
            ReleaseDetailResponse>(
            "api/v1/admin/releases",
            new CreateReleaseRequest
            {
                ProductCode = "PCCAM",
                Version = "1.2.3",
                Channel = "stable",
                IsMandatory = true
            });

        Assert.True(result.Success);
        Assert.Contains("\"productCode\":\"PCCAM\"", handler.RequestBody);
        Assert.Contains("\"isMandatory\":true", handler.RequestBody);
        Assert.DoesNotContain("ProductCode", handler.RequestBody);
    }

    [Fact]
    public async Task 오류응답의_HTTP상태_ErrorCode_RequestId를_보존한다()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(
            HttpStatusCode.Conflict)
        {
            Headers =
            {
                { "X-Request-ID", "request-409" }
            },
            Content = JsonContent.Create(new ApiResponse<object>
            {
                Success = false,
                Message = "동일한 릴리스가 존재합니다.",
                ErrorCode = 8011
            })
        });
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient, "account-token");

        var result = await client.PostAsync<
            CreateReleaseRequest,
            ReleaseDetailResponse>(
            "api/v1/admin/releases",
            new CreateReleaseRequest());

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
        Assert.Equal(8011, result.ErrorCode);
        Assert.Equal("request-409", result.RequestId);
        Assert.True(result.IsConflict);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, 5004, true, false)]
    [InlineData(HttpStatusCode.Forbidden, 7001, false, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, 9003, false, false)]
    public async Task 인증_권한_외부서비스상태를_구분한다(
        HttpStatusCode statusCode,
        int errorCode,
        bool expectedUnauthorized,
        bool expectedForbidden)
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(statusCode)
        {
            Content = JsonContent.Create(new ApiResponse<object>
            {
                Success = false,
                Message = "요청 실패",
                ErrorCode = errorCode
            })
        });
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient, "account-token");

        var result = await client.GetAsync<ReleaseDetailResponse>(
            "api/v1/admin/releases/1");

        Assert.Equal(expectedUnauthorized, result.IsUnauthorized);
        Assert.Equal(expectedForbidden, result.IsForbidden);
        Assert.Equal(
            statusCode == HttpStatusCode.ServiceUnavailable,
            result.IsServiceUnavailable);
    }

    [Fact]
    public async Task 비JSON오류응답은_원본문서대신_안전한메시지를_반환한다()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(
            HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("<html>secret stack trace</html>")
        });
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient, "account-token");

        var result = await client.GetAsync<ReleaseDetailResponse>(
            "api/v1/admin/releases/1");

        Assert.False(result.Success);
        Assert.DoesNotContain("secret", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("html", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Token이없으면_HTTP요청을_보내지않는다()
    {
        var handler = new RecordingHandler(_ => throw new InvalidOperationException());
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient, token: null);

        var result = await client.GetAsync<ReleaseDetailResponse>(
            "api/v1/admin/releases/1");

        Assert.True(result.IsUnauthorized);
        Assert.Equal(0, handler.CallCount);
    }

    private static UpdateApiClient CreateClient(
        HttpClient httpClient,
        string? token)
    {
        return new UpdateApiClient(
            httpClient,
            new AuthStateService(new SessionStorageJsRuntime(token)));
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://update.internal/")
        };
    }

    private static HttpResponseMessage SuccessResponse<T>(T data)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new ApiResponse<T>
            {
                Success = true,
                Message = "성공",
                ErrorCode = 0,
                Data = data
            })
        };
    }

    private sealed class SessionStorageJsRuntime : IJSRuntime
    {
        private readonly string? _token;

        public SessionStorageJsRuntime(string? token)
        {
            _token = token;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            object?[]? args)
        {
            return InvokeAsync<TValue>(
                identifier,
                CancellationToken.None,
                args);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            object? result = identifier == "sessionStorage.getItem"
                ? _token
                : null;

            return ValueTask.FromResult((TValue)result!);
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _factory;

        public RecordingHandler(
            Func<HttpRequestMessage, HttpResponseMessage> factory)
        {
            _factory = factory;
        }

        public int CallCount { get; private set; }

        public string? AuthorizationToken { get; private set; }

        public Uri? RequestUri { get; private set; }

        public string RequestBody { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            AuthorizationToken = request.Headers.Authorization?.Parameter;
            RequestUri = request.RequestUri;
            RequestBody = request.Content is null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return _factory(request);
        }
    }
}
