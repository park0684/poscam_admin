using Microsoft.AspNetCore.Mvc;
using poscam.AuthServer.Controllers;
using Xunit;

namespace poscam.AuthServer.Tests.Services;

public class UpdateApiContractTests
{
    [Fact]
    public void CurrentAccessEndpoint_IsGetWithoutUserCodeInput()
    {
        var method = typeof(AccountController).GetMethod(nameof(AccountController.GetCurrentAccess));

        Assert.NotNull(method);
        Assert.Empty(method.GetParameters());
        var httpGet = Assert.Single(method.GetCustomAttributes(typeof(HttpGetAttribute), inherit: true)
            .Cast<HttpGetAttribute>());
        Assert.Equal("api/accounts/me/access", httpGet.Template);
    }

    [Fact]
    public void InternalAuthorizeEndpoint_IsPostWithOnlyServiceKeyHeaderInput()
    {
        var method = typeof(InternalUpdateManagementController)
            .GetMethod(nameof(InternalUpdateManagementController.Authorize));

        Assert.NotNull(method);
        var parameter = Assert.Single(method.GetParameters());
        Assert.Equal("serviceKey", parameter.Name);
        var fromHeader = Assert.Single(parameter.GetCustomAttributes(typeof(FromHeaderAttribute), inherit: true)
            .Cast<FromHeaderAttribute>());
        Assert.Equal("X-POSCAM-Service-Key", fromHeader.Name);
        var httpPost = Assert.Single(method.GetCustomAttributes(typeof(HttpPostAttribute), inherit: true)
            .Cast<HttpPostAttribute>());
        Assert.Equal("authorize", httpPost.Template);
    }
}
