using System.Security.Cryptography;

namespace poscam.AuthServer.Services;

/// <summary>
/// 관리자/담당자 계정 비밀번호 해시 서비스.
/// 
/// users.user_password_hash 컬럼에 저장할 값을 생성하고 검증한다.
/// PBKDF2 기반으로 처리하며, 저장 형식은 아래와 같다.
/// 
/// PBKDF2$반복횟수$saltBase64$hashBase64
/// </summary>
public class PasswordHashService
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    /// <summary>
    /// 평문 비밀번호를 해시 문자열로 변환한다.
    /// </summary>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("비밀번호가 비어 있습니다.", nameof(password));
        }

        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return $"PBKDF2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    /// <summary>
    /// 입력 비밀번호가 저장된 해시와 일치하는지 검증한다.
    /// </summary>
    public bool VerifyPassword(string inputPassword, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(inputPassword))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var parts = storedHash.Split('$');

        if (parts.Length != 4)
        {
            return false;
        }

        if (!string.Equals(parts[0], "PBKDF2", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        byte[] salt;
        byte[] expectedHash;

        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expectedHash = Convert.FromBase64String(parts[3]);
        }
        catch
        {
            return false;
        }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            inputPassword,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}