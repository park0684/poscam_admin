using Microsoft.Extensions.Options;
using poscam.AdminWeb.Models;
using poscam.AdminWeb.Services;
using poscam.AdminWeb.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<ApiSettings>(
    builder.Configuration.GetSection("ApiSettings"));

builder.Services.AddScoped<AuthStateService>();

builder.Services.AddHttpClient<ApiClient>((serviceProvider, client) =>
{
    var apiSettings = serviceProvider
        .GetRequiredService<IOptions<ApiSettings>>()
        .Value;

    client.BaseAddress = new Uri(apiSettings.BaseUrl);
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