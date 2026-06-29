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

builder.Services
    .AddOptions<UpdateApiSettings>()
    .Bind(builder.Configuration.GetSection(UpdateApiSettings.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        settings =>
            Uri.TryCreate(settings.InternalBaseUrl, UriKind.Absolute, out var internalUri)
            && (internalUri.Scheme == Uri.UriSchemeHttp
                || internalUri.Scheme == Uri.UriSchemeHttps),
        "UpdateApiSettings:InternalBaseUrl은 유효한 HTTP 또는 HTTPS 절대 URL이어야 합니다.")
    .Validate(
        settings =>
            Uri.TryCreate(settings.PublicBaseUrl, UriKind.Absolute, out var publicUri)
            && (publicUri.Scheme == Uri.UriSchemeHttp
                || publicUri.Scheme == Uri.UriSchemeHttps),
        "UpdateApiSettings:PublicBaseUrl은 유효한 HTTP 또는 HTTPS 절대 URL이어야 합니다.")
    .ValidateOnStart();

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

// UpdateServer JSON API는 AuthServer ApiClient와 BaseAddress·오류 계약을
// 공유하지 않는다. C03의 browser 직접 업로드에는 PublicBaseUrl을 사용한다.
builder.Services.AddHttpClient<UpdateApiClient>((serviceProvider, client) =>
{
    var updateApiSettings = serviceProvider
        .GetRequiredService<IOptions<UpdateApiSettings>>()
        .Value;

    client.BaseAddress = new Uri(
        updateApiSettings.InternalBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

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
