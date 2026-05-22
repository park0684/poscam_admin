using Microsoft.AspNetCore.Mvc;
using poscam.AuthServer.Models.Dtos.Account;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Repositories;
using poscam.AuthServer.Services;

namespace poscam.AuthServer.Controllers;

/// <summary>
/// 개발 환경 전용 Controller.
/// 
/// 초기 관리자 계정 생성 등 개발 편의 기능만 포함한다.
/// 운영 환경에서는 실행되지 않도록 환경 체크를 반드시 수행한다.
/// </summary>
[ApiController]
[Route("api/dev")]
public class DevController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly UserAccountRepository _userAccountRepository;
    private readonly PasswordHashService _passwordHashService;

    public DevController(
        IWebHostEnvironment environment,
        UserAccountRepository userAccountRepository,
        PasswordHashService passwordHashService)
    {
        _environment = environment;
        _userAccountRepository = userAccountRepository;
        _passwordHashService = passwordHashService;
    }

    /// <summary>
    /// 개발용 초기 관리자 계정 생성 API.
    /// 
    /// ID: admin
    /// PW: admin1234
    /// 
    /// 이미 admin 계정이 있으면 비밀번호 해시와 상태를 갱신한다.
    /// Development 환경에서만 실행된다.
    /// </summary>
    [HttpPost("seed-admin")]
    [ProducesResponseType(typeof(ApiResponse<DevSeedAdminResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DevSeedAdminResponse>>> SeedAdmin()
    {
        if (!_environment.IsDevelopment())
        {
            return Ok(ApiResponse<DevSeedAdminResponse>.Fail(
                AuthErrorCode.InvalidLogin,
                "개발 환경에서만 사용할 수 있는 API입니다."));
        }

        const string adminId = "1";
        const string adminPassword = "1";

        var passwordHash = _passwordHashService.HashPassword(adminPassword);

        var existingAdmin = await _userAccountRepository.GetByUserIdAsync(adminId);

        if (existingAdmin == null)
        {
            var admin = new UserAccount
            {
                PartnerCode = null,
                UserId = adminId,
                UserPasswordHash = passwordHash,
                UserName = "시스템 관리자",
                UserCell = null,
                UserEmail = null,
                UserRole = (int)UserRole.Admin,
                UserStatus = (int)UserStatus.Active,
                ApprovedBy = null
            };

            var userCode = await _userAccountRepository.InsertAdminAsync(admin);

            return Ok(ApiResponse<DevSeedAdminResponse>.Ok(
                new DevSeedAdminResponse
                {
                    UserCode = userCode,
                    UserId = adminId,
                    Password = adminPassword,
                    Created = true,
                    Updated = false
                },
                "개발용 관리자 계정이 생성되었습니다."));
        }

        await _userAccountRepository.UpdateAdminPasswordAndActivateAsync(
            existingAdmin.UserCode,
            passwordHash);

        return Ok(ApiResponse<DevSeedAdminResponse>.Ok(
            new DevSeedAdminResponse
            {
                UserCode = existingAdmin.UserCode,
                UserId = adminId,
                Password = adminPassword,
                Created = false,
                Updated = true
            },
            "기존 개발용 관리자 계정이 갱신되었습니다."));
    }
}