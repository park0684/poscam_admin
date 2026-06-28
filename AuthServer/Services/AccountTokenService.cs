using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using poscam.AuthServer.Models.Dtos.Account;
using poscam.AuthServer.Models.Entities;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Options;

namespace poscam.AuthServer.Services;

/// <summary>
/// 관리자/담당자 로그인 토큰 발급 및 검증 서비스.
/// 
/// PC캠/캠뷰어 인증 토큰과 목적이 다르므로
/// 기존 TokenService와 분리해서 사용한다.
/// </summary>
public class AccountTokenService
{
    private readonly AuthPolicyOptions _options;

    public AccountTokenService(IOptions<AuthPolicyOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// 사용자 계정 정보를 기준으로 관리자 웹용 토큰을 발급한다.
    /// </summary>
    public string CreateToken(UserAccount user)
    {
        var now = DateTime.UtcNow;

        var payload = new AccountTokenPayloadDto
        {
            UserCode = user.UserCode,
            PartnerCode = user.PartnerCode,
            UserId = user.UserId,
            UserName = user.UserName,
            UserRole = user.UserRole,
            UserStatus = user.UserStatus,
            IssuedAt = now,
            ExpiresAt = now.AddHours(_options.AccountTokenExpireHours)
        };

        return SignPayload(payload);
    }

    /// <summary>
    /// Authorization 헤더 값을 받아 Bearer 토큰을 추출한다.
    /// </summary>
    public string? ExtractBearerToken(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            return null;
        }

        const string prefix = "Bearer ";

        if (!authorizationHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return authorizationHeader.Substring(prefix.Length).Trim();
    }

    /// <summary>
    /// 관리자/담당자 토큰을 검증하고 Payload를 복원한다.
    /// 
    /// 여기서는 토큰 자체만 검증한다.
    /// 사용자가 현재도 Active인지 여부는 AccountService 또는 각 관리 Service에서
    /// users 테이블을 다시 조회하여 확인한다.
    /// </summary>
    public AccountTokenValidationResult ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return AccountTokenValidationResult.Fail(
                AuthErrorCode.InvalidLogin,
                "토큰이 없습니다.");
        }

        var parts = token.Split('.');

        if (parts.Length != 2)
        {
            return AccountTokenValidationResult.Fail(
                AuthErrorCode.TokenInvalid,
                "토큰 형식이 올바르지 않습니다.");
        }

        var payloadBase64 = parts[0];
        var signatureBase64 = parts[1];

        var expectedSignature = Base64UrlEncode(Sign(payloadBase64));

        if (!FixedTimeEquals(signatureBase64, expectedSignature))
        {
            return AccountTokenValidationResult.Fail(
                AuthErrorCode.TokenInvalid,
                "토큰 서명이 올바르지 않습니다.");
        }

        AccountTokenPayloadDto? payload;

        try
        {
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(payloadBase64));
            payload = JsonSerializer.Deserialize<AccountTokenPayloadDto>(payloadJson);
        }
        catch
        {
            return AccountTokenValidationResult.Fail(
                AuthErrorCode.TokenInvalid,
                "토큰 내용을 읽을 수 없습니다.");
        }

        if (payload == null)
        {
            return AccountTokenValidationResult.Fail(
                AuthErrorCode.TokenInvalid,
                "토큰 정보가 비어 있습니다.");
        }

        if (payload.ExpiresAt < DateTime.UtcNow)
        {
            return AccountTokenValidationResult.Fail(
                AuthErrorCode.TokenExpired,
                "토큰이 만료되었습니다.");
        }

        return AccountTokenValidationResult.Success(payload);
    }

    /// <summary>
    /// Payload JSON을 HMAC-SHA256으로 서명하여 토큰 문자열을 만든다.
    /// </summary>
    private string SignPayload(AccountTokenPayloadDto payload)
    {
        var json = JsonSerializer.Serialize(payload);

        var payloadBytes = Encoding.UTF8.GetBytes(json);
        var payloadBase64 = Base64UrlEncode(payloadBytes);

        var signatureBytes = Sign(payloadBase64);
        var signatureBase64 = Base64UrlEncode(signatureBytes);

        return $"{payloadBase64}.{signatureBase64}";
    }

    /// <summary>
    /// HMAC-SHA256 서명을 생성한다.
    /// </summary>
    private byte[] Sign(string payloadBase64)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_options.TokenSecret);

        using var hmac = new HMACSHA256(keyBytes);

        return hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadBase64));
    }

    /// <summary>
    /// Base64Url 인코딩.
    /// </summary>
    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>
    /// Base64Url 디코딩.
    /// </summary>
    private static byte[] Base64UrlDecode(string value)
    {
        var base64 = value
            .Replace('-', '+')
            .Replace('_', '/');

        switch (base64.Length % 4)
        {
            case 2:
                base64 += "==";
                break;
            case 3:
                base64 += "=";
                break;
            case 0:
                break;
            default:
                throw new FormatException("Base64Url 형식이 올바르지 않습니다.");
        }

        return Convert.FromBase64String(base64);
    }

    /// <summary>
    /// 문자열 비교 시 타이밍 공격 가능성을 줄이기 위한 고정 시간 비교.
    /// </summary>
    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        return leftBytes.Length == rightBytes.Length &&
               CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
