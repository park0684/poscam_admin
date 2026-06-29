using System.Net;
using System.Net.Http.Json;
using Microsoft.JSInterop;
using poscam.AdminWeb.Models.Account;
using poscam.AdminWeb.Models.Common;
using poscam.AdminWeb.Services;
using Xunit;

namespace poscam.AdminWeb.Tests.Services;

public class CurrentUserAccessPolicyTests
{
    [Fact]
    public void CanManageUpdates_System은_권한목록없이_허용한다()
    {
        var access = CreateAccess(CurrentUserAccessPolicy.SystemRole);

        Assert.True(CurrentUserAccessPolicy.CanManageUpdates(access));
    }

    [Fact]
    public void CanManageUpdates_Admin은_권한12가_있을때만_허용한다()
    {
        var allowed = CreateAccess(
            CurrentUserAccessPolicy.AdminRole,
            CurrentUserAccessPolicy.UpdateManagePermissionCode);
        var denied = CreateAccess(CurrentUserAccessPolicy.AdminRole);

        Assert.True(CurrentUserAccessPolicy.CanManageUpdates(allowed));
        Assert.False(CurrentUserAccessPolicy.CanManageUpdates(denied));
    }

    [Theory]
    [InlineData(CurrentUserAccessPolicy.PartnerUserRole)]
    [InlineData(3)]
    public void CanManageUpdates_Admin이외역할은_권한12가있어도_거부한다(int role)
    {
        var access = CreateAccess(
            role,
            CurrentUserAccessPolicy.UpdateManagePermissionCode);

        Assert.False(CurrentUserAccessPolicy.CanManageUpdates(access));
    }

    [Fact]
    public void CanManageUpdates_접근정보가없으면_거부한다()
    {
        Assert.False(CurrentUserAccessPolicy.CanManageUpdates(null));
    }

    private static CurrentUserAccessResponse CreateAccess(
        int role,
        params int[] permissionCodes)
    {
        return new CurrentUserAccessResponse
        {
            UserCode = 1,
            UserName = "테스트 사용자",
            UserRole = role,
            PermissionCodes = permissionCodes.ToList()
        };
    }
}

public class CurrentUserAccessServiceTests
{
    [Fact]
    public async Task GetCurrentAccessAsync_동일Token의_성공응답을_한번만조회한다()
    {
        var jsRuntime = new SessionStorageJsRuntime("token-a");
        var handler = new StubHttpMessageHandler(_ => SuccessResponse());
        using var service = CreateService(jsRuntime, handler);

        var first = await service.GetCurrentAccessAsync();
        var second = await service.GetCurrentAccessAsync();

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(1, handler.CallCount);
        Assert.Equal("token-a", handler.AuthorizationTokens.Single());
    }

    [Fact]
    public async Task GetCurrentAccessAsync_Token이바뀌면_캐시를버리고_다시조회한다()
    {
        var jsRuntime = new SessionStorageJsRuntime("token-a");
        var handler = new StubHttpMessageHandler(_ => SuccessResponse());
        using var service = CreateService(jsRuntime, handler);

        await service.GetCurrentAccessAsync();
        jsRuntime.Token = "token-b";
        await service.GetCurrentAccessAsync();

        Assert.Equal(2, handler.CallCount);
        Assert.Equal(new[] { "token-a", "token-b" }, handler.AuthorizationTokens);
    }

    [Fact]
    public async Task Invalidate_다음호출에서_접근정보를_다시조회한다()
    {
        var jsRuntime = new SessionStorageJsRuntime("token-a");
        var handler = new StubHttpMessageHandler(_ => SuccessResponse());
        using var service = CreateService(jsRuntime, handler);

        await service.GetCurrentAccessAsync();
        service.Invalidate();
        await service.GetCurrentAccessAsync();

        Assert.Equal(2, handler.CallCount);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, CurrentUserAccessStatus.Unauthenticated)]
    [InlineData(HttpStatusCode.Forbidden, CurrentUserAccessStatus.Forbidden)]
    public async Task GetCurrentAccessAsync_401과403을_구분한다(
        HttpStatusCode httpStatusCode,
        CurrentUserAccessStatus expectedStatus)
    {
        var jsRuntime = new SessionStorageJsRuntime("token-a");
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(httpStatusCode)
        {
            Content = JsonContent.Create(new ApiResponse<CurrentUserAccessResponse>
            {
                Success = false,
                Message = "접근 거부",
                ErrorCode = httpStatusCode == HttpStatusCode.Unauthorized
                    ? 5004
                    : 7001
            })
        });
        using var service = CreateService(jsRuntime, handler);

        var result = await service.GetCurrentAccessAsync();

        Assert.Equal(expectedStatus, result.Status);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task GetCurrentAccessAsync_Token이없으면_API를호출하지않는다()
    {
        var jsRuntime = new SessionStorageJsRuntime(token: null);
        var handler = new StubHttpMessageHandler(_ => SuccessResponse());
        using var service = CreateService(jsRuntime, handler);

        var result = await service.GetCurrentAccessAsync();

        Assert.Equal(CurrentUserAccessStatus.Unauthenticated, result.Status);
        Assert.Equal(0, handler.CallCount);
    }

    private static CurrentUserAccessService CreateService(
        SessionStorageJsRuntime jsRuntime,
        StubHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://auth.example.com/")
        };

        return new CurrentUserAccessService(
            httpClient,
            new AuthStateService(jsRuntime));
    }

    private static HttpResponseMessage SuccessResponse()
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new ApiResponse<CurrentUserAccessResponse>
            {
                Success = true,
                Message = "현재 사용자 접근정보를 조회했습니다.",
                ErrorCode = 0,
                Data = new CurrentUserAccessResponse
                {
                    UserCode = 15,
                    UserName = "운영 관리자",
                    UserRole = CurrentUserAccessPolicy.AdminRole,
                    PermissionCodes = new List<int>
                    {
                        CurrentUserAccessPolicy.UpdateManagePermissionCode,
                        CurrentUserAccessPolicy.UpdateManagePermissionCode
                    }
                }
            })
        };
    }

    private sealed class SessionStorageJsRuntime : IJSRuntime
    {
        public SessionStorageJsRuntime(string? token)
        {
            Token = token;
        }

        public string? Token { get; set; }

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
            object? result = identifier switch
            {
                "sessionStorage.getItem" => Token,
                "sessionStorage.setItem" => null,
                "sessionStorage.removeItem" => null,
                _ => throw new InvalidOperationException(
                    $"지원하지 않는 JS 호출입니다: {identifier}")
            };

            return ValueTask.FromResult((TValue)result!);
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(
            Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public int CallCount { get; private set; }

        public List<string?> AuthorizationTokens { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            AuthorizationTokens.Add(
                request.Headers.Authorization?.Parameter);

            return Task.FromResult(_responseFactory(request));
        }
    }
}
