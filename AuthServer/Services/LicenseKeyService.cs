using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using poscam.AuthServer.Options;

namespace poscam.AuthServer.Services;

/// <summary>
/// 라이선스 키 생성과 입력 보정을 담당하는 서비스.
/// 
/// 라이선스 키는 백엔드에서 생성한다.
/// 매장코드와 계약코드는 키 문자열에 직접 노출하지 않고,
/// HMAC 입력 조건값으로만 반영한다.
/// </summary>
public class LicenseKeyService
{
    private readonly AuthPolicyOptions _options;

    /// <summary>
    /// 혼동 문자를 제외한 인증키 문자셋.
    /// 제외 문자: 0, 1, I, O, L, Q, |
    /// </summary>
    private const string AllowedChars = "23456789ABCDEFGHJKMNPRSTUVWXYZ";

    public LicenseKeyService(IOptions<AuthPolicyOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// PC 캠 라이선스 키를 생성한다.
    /// 
    /// 라이선스는 계약 기준으로 발급되며,
    /// 매장 연결 여부와 무관하게 발급 가능해야 한다.
    /// 
    /// 내부 조건값:
    /// - 계약코드
    /// - 발급순번
    /// - 암호학적 난수
    /// </summary>
    public string GeneratePccamLicenseKey(
        int contractCode,
        int issueSequence)
    {
        var nonce = RandomNumberGenerator.GetBytes(16);
        var nonceText = Convert.ToHexString(nonce);

        var rawText = $"{contractCode}:{issueSequence}:{nonceText}";

        var keyBytes = Encoding.UTF8.GetBytes(_options.TokenSecret);
        var rawBytes = Encoding.UTF8.GetBytes(rawText);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(rawBytes);

        var body = ConvertHashToLicenseBody(hash, 12);

        return FormatWithPrefix(_options.PccamLicensePrefix, body);
    }

    /// <summary>
    /// 사용자가 입력한 인증키를 서버 기준 형식으로 보정한다.
    /// 
    /// 예:
    /// pcma8k27m9pw3x4 → PCM-A8K2-7M9P-W3X4
    /// </summary>
    public string NormalizeLicenseKey(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "";
        }

        var prefix = _options.PccamLicensePrefix.ToUpperInvariant();

        var cleaned = new StringBuilder();

        foreach (var ch in input.Trim().ToUpperInvariant())
        {
            if (ch == '-' || char.IsWhiteSpace(ch))
            {
                continue;
            }

            cleaned.Append(ch);
        }

        var value = cleaned.ToString();

        if (value.StartsWith(prefix))
        {
            value = value.Substring(prefix.Length);
        }

        return FormatWithPrefix(prefix, value);
    }

    /// <summary>
    /// PC 캠 인증키 형식이 유효한지 확인한다.
    /// 
    /// 이 메서드는 형식만 검사한다.
    /// DB 존재 여부와 사용 가능 여부는 인증 Service에서 판단한다.
    /// </summary>
    public bool IsValidPccamLicenseKey(string licenseKey)
    {
        var normalized = NormalizeLicenseKey(licenseKey);
        var prefix = _options.PccamLicensePrefix.ToUpperInvariant();

        if (!normalized.StartsWith(prefix + "-"))
        {
            return false;
        }

        var body = normalized.Replace(prefix + "-", "").Replace("-", "");

        if (body.Length != 12)
        {
            return false;
        }

        return body.All(ch => AllowedChars.Contains(ch));
    }

    /// <summary>
    /// 해시 바이트를 인증키 문자셋으로 변환한다.
    /// </summary>
    private static string ConvertHashToLicenseBody(byte[] hash, int length)
    {
        var result = new StringBuilder(length);

        for (var i = 0; i < length; i++)
        {
            var index = hash[i] % AllowedChars.Length;
            result.Append(AllowedChars[index]);
        }

        return result.ToString();
    }

    /// <summary>
    /// 접두어와 본문을 표준 인증키 형식으로 변환한다.
    /// </summary>
    private static string FormatWithPrefix(string prefix, string body)
    {
        prefix = prefix.ToUpperInvariant();
        body = body.ToUpperInvariant().Replace("-", "").Trim();

        if (body.Length != 12)
        {
            return $"{prefix}-{body}";
        }

        return $"{prefix}-{body.Substring(0, 4)}-{body.Substring(4, 4)}-{body.Substring(8, 4)}";
    }
}