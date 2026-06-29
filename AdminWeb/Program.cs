using Microsoft.Extensions.Options;
using poscam.AdminWeb.Models;
using poscam.AdminWeb.Services;
using poscam.AdminWeb.Components;

var builder = WebApplication.CreateBuilder(args);

const string currentUserAccessHttpClientName = "CurrentUserAccess";

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<ApiSettings>(
    builder.Configuration.GetSection("ApiSettings"));

builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<MenuAccessFilter>();

builder.Services.AddHttpClient<ApiClient>((serviceProvider, client) =>
{
    var apiSettings = serviceProvider
        .GetRequiredService<IOptions<ApiSettings>>()
        .Value;

    client.BaseAddress = new Uri(apiSettings.BaseUrl);
});

// 현재 사용자 접근정보는 AuthServer를 호출하지만 기존 ApiClient와
// HTTP 상태 처리 및 캐시 수명이 다르므로 별도 named client를 사용한다.
builder.Services.AddHttpClient(currentUserAccessHttpClientName, (serviceProvider, client) =>
{
    var apiSettings = serviceProvider
        .GetRequiredService<IOptions<ApiSettings>>()
        .Value;

    client.BaseAddress = new Uri(apiSettings.BaseUrl);
});

// AddHttpClient<T>의 기본 수명은 Transient이므로, 접근정보 캐시가
// Blazor 회로의 Scoped 수명 동안 유지되도록 명시적으로 Scoped 등록한다.
builder.Services.AddScoped(serviceProvider =>
    new CurrentUserAccessService(
        serviceProvider
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(currentUserAccessHttpClientName),
        serviceProvider.GetRequiredService<AuthStateService>()));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
