namespace poscam.AuthServer.Services;

/// <summary>
/// 비밀번호 검증 서비스.
/// 
/// 현재는 평문 비교 방식으로 시작한다.
/// 추후 BCrypt, PBKDF2, Argon2 등의 해시 방식으로 전환할 때
/// 이 클래스만 수정하면 된다.
/// </summary>
public class PasswordService
{
    /// <summary>
    /// 매장 비밀번호를 검증한다.
    /// 
    /// 현재는 평문 비교 기준이다.
    /// 운영 단계에서는 반드시 해시 검증 방식으로 변경해야 한다.
    /// </summary>
    public bool VerifyStorePassword(string inputPassword, string storedPassword)
    {
        if (string.IsNullOrEmpty(inputPassword))
        {
            return false;
        }

        if (string.IsNullOrEmpty(storedPassword))
        {
            return false;
        }

        return inputPassword == storedPassword;
    }

    /// <summary>
    /// 저장용 비밀번호를 반환한다.
    /// 
    /// 현재는 평문 그대로 반환한다.
    /// 추후 해시 방식으로 전환하면 이 메서드에서 해시 처리한다.
    /// </summary>
    public string CreateStorePasswordValue(string plainPassword)
    {
        return plainPassword;
    }
}