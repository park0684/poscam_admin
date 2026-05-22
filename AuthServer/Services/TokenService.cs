using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using poscam.AuthServer.Models.Dtos.Common;
using poscam.AuthServer.Models.Enums;
using poscam.AuthServer.Options;

namespace poscam.AuthServer.Services;

/// <summary>
/// 인증 토큰 발급 및 검증 서비스.
/// 
/// PC캠과 캠뷰어 모두 이 서비스를 통해 토큰을 발급받는다.
/// 캠뷰어는 최초 로그인 이후 토큰으로 실행 인증을 수행한다.
/// </summary>
public class TokenService
{
    private readonly AuthPolicyOptions _options;

    public TokenService(IOptions<AuthPolicyOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// 인증 토큰을 생성한다.
    /// 
    /// deviceCode를 토큰에 포함해야 나중에 devices 테이블에서
    /// 해당 장비가 아직 사용 가능한 장비인지 확인할 수 있다.
    /// </summary>
    public AuthTokenDto CreateToken(
        int? storeCode,
        int contractCode,
        int? licenseCode,
        int deviceCode,
        DeviceAppType appType,
        string hwid,
        ContractType contractType,
        bool isPermanent,
        string? configVersion = null)
    {
        var now = DateTime.UtcNow;

        var expiresAt = isPermanent
            ? now.AddYears(30)
            : now.AddHours(_options.TokenExpireHours);

        var offlineUntil = isPermanent
            ? now.AddYears(30)
        : appType == DeviceAppType.Pccam
            ? now.AddDays(_options.PccamOfflineDays)
            : CalculateOfflineUntil(now, contractType);

        var payload = new AuthTokenPayloadDto
        {
            StoreCode = storeCode,
            ContractCode = contractCode,
            LicenseCode = licenseCode,
            DeviceCode = deviceCode,
            AppType = (int)appType,
            Hwid = hwid,
            IssuedAt = now,
            ExpiresAt = expiresAt,
            OfflineUntil = offlineUntil,
            IsPermanent = isPermanent,
            ConfigVersion = configVersion
        };

        var token = SignPayload(payload);

        return new AuthTokenDto
        {
            Token = token,
            IssuedAt = now,
            ExpiresAt = expiresAt,
            OfflineUntil = offlineUntil,
            IsPermanent = isPermanent,
            ConfigVersion = configVersion
        };
    }

    /// <summary>
    /// 토큰을 검증하고 Payload를 복원한다.
    /// 
    /// 검증 항목:
    /// 1. 토큰 존재 여부
    /// 2. 토큰 형식
    /// 3. 서명 일치 여부
    /// 4. Payload 역직렬화
    /// 5. 만료 여부
    /// 
    /// HWID 비교, deviceCode 존재 여부,
    /// 라이선스/계약 상태 검증은 각 인증 Service에서 처리한다.
    /// </summary>
    public TokenValidationResult ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return TokenValidationResult.Fail(
                AuthErrorCode.TokenInvalid,
                "토큰이 없습니다.");
        }

        var parts = token.Split('.');

        if (parts.Length != 2)
        {
            return TokenValidationResult.Fail(
                AuthErrorCode.TokenInvalid,
                "토큰 형식이 올바르지 않습니다.");
        }

        var payloadBase64 = parts[0];
        var signatureBase64 = parts[1];

        var expectedSignature = Base64UrlEncode(Sign(payloadBase64));

        if (!FixedTimeEquals(signatureBase64, expectedSignature))
        {
            return TokenValidationResult.Fail(
                AuthErrorCode.TokenInvalid,
                "토큰 서명이 올바르지 않습니다.");
        }

        AuthTokenPayloadDto? payload;

        try
        {
            var payloadJson = Encoding.UTF8.GetString(
                Base64UrlDecode(payloadBase64));

            payload = JsonSerializer.Deserialize<AuthTokenPayloadDto>(
                payloadJson);
        }
        catch
        {
            return TokenValidationResult.Fail(
                AuthErrorCode.TokenInvalid,
                "토큰 내용을 읽을 수 없습니다.");
        }

        if (payload == null)
        {
            return TokenValidationResult.Fail(
                AuthErrorCode.TokenInvalid,
                "토큰 정보가 비어 있습니다.");
        }

        if (!payload.IsPermanent &&
            payload.ExpiresAt < DateTime.UtcNow)
        {
            return TokenValidationResult.Fail(
                AuthErrorCode.TokenExpired,
                "토큰이 만료되었습니다.");
        }

        return TokenValidationResult.Success(payload);
    }

    /// <summary>
    /// 계약 유형에 따라 오프라인 허용 만료 시각을 계산한다.
    /// </summary>
    private DateTime CalculateOfflineUntil(DateTime now, ContractType contractType)
    {
        return contractType switch
        {
            ContractType.Trial => now.AddDays(_options.TrialOfflineDays),
            ContractType.Subscription => now.AddDays(_options.SubscriptionOfflineDays),
            ContractType.Purchase => now.AddDays(_options.PurchaseOfflineDays),
            _ => now.AddDays(1)
        };
    }

    /// <summary>
    /// Payload JSON을 HMAC-SHA256으로 서명하여 토큰 문자열을 만든다.
    /// </summary>
    private string SignPayload(AuthTokenPayloadDto payload)
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