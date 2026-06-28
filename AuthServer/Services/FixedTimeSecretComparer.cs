using System.Security.Cryptography;
using System.Text;

namespace poscam.AuthServer.Services;

/// <summary>
/// 민감한 문자열을 고정 길이 해시로 변환한 뒤 고정시간 비교한다.
/// </summary>
public static class FixedTimeSecretComparer
{
    public static bool MatchesConfiguredSecret(
        string? providedSecret,
        string? expectedSecret,
        string placeholder)
    {
        if (string.IsNullOrWhiteSpace(providedSecret) ||
            string.IsNullOrWhiteSpace(expectedSecret) ||
            string.Equals(expectedSecret, placeholder, StringComparison.Ordinal))
        {
            return false;
        }

        var providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(providedSecret));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expectedSecret));

        return CryptographicOperations.FixedTimeEquals(providedHash, expectedHash);
    }
}
