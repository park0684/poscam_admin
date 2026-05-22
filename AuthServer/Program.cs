using Dapper;
using poscam.AuthServer.Options;
using poscam.AuthServer.Repositories;
using poscam.AuthServer.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Dapper에서 store_code → StoreCode 형태의 매핑을 허용한다.
DefaultTypeMap.MatchNamesWithUnderscores = true;

builder.Services.Configure<AuthPolicyOptions>(
    builder.Configuration.GetSection("AuthPlicy"));

builder.Services.AddSingleton<IDbContext, DapperContext>();

// Repository 등록
builder.Services.AddScoped<StoreRepository>();
builder.Services.AddScoped<ContractRepository>();
builder.Services.AddScoped<LicenseKeyRepository>();
builder.Services.AddScoped<DeviceRepository>();
builder.Services.AddScoped<NvrConfigRepository>();
builder.Services.AddScoped<ChannelConfigRepository>();
builder.Services.AddScoped<AuthLogRepository>();
builder.Services.AddScoped<LicenseLogRepository>();
builder.Services.AddScoped<UserAccountRepository>();
builder.Services.AddScoped<PartnerRepository>();
builder.Services.AddScoped<StoreAssignmentRepository>();
builder.Services.AddScoped<UserLogRepository>();
builder.Services.AddScoped<PartnerPricePolicyRepository>();
builder.Services.AddScoped<ContractBillingRepository>();
builder.Services.AddScoped<BillingPaymentRepository>();
builder.Services.AddScoped<AdminUserPermissionRepository>();


// Service
builder.Services.AddScoped<LicenseKeyService>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<PasswordHashService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AccountTokenService>();
builder.Services.AddScoped<CodeGenerateService>();
builder.Services.AddScoped<StoreManageService>();
builder.Services.AddScoped<PartnerService>();
builder.Services.AddScoped<ContractManageService>();
builder.Services.AddScoped<LicenseManageService>();
builder.Services.AddScoped<ConfigManageService>();
builder.Services.AddScoped<UserManageService>();
builder.Services.AddScoped<SettlementService>();
builder.Services.AddScoped<AdminPermissionService>();
builder.Services.AddScoped<AdminAccountManageService>();

builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<PccamAuthService>();
builder.Services.AddScoped<ViewerAuthService>();
builder.Services.AddScoped<ConfigService>();
builder.Services.AddScoped<DeviceService>();

//swagger 테스트를 위한 임시 수정
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "관리자 로그인 후 발급받은 토큰을 입력하세요.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "AccountToken"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    success = true,
    message = "poscam.AuthServer is running",
    serverTime = DateTimeOffset.UtcNow
}));

app.Run();
