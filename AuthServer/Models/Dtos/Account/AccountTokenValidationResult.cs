using poscam.AuthServer.Models.Enums;

namespace poscam.AuthServer.Models.Dtos.Account;

/// <summary>
/// 관리자/담당자 토큰 검증 결과 DTO.
/// </summary>
public class AccountTokenValidationResult
{
    /// <summary>
    /// 토큰 유효 여부.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 실패 시 오류 코드.
    /// </summary>
    public AuthErrorCode ErrorCode { get; set; } = AuthErrorCode.None;

    /// <summary>
    /// 검증 결과 메시지.
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// 토큰 검증 성공 시 복원된 Payload.
    /// </summary>
    public AccountTokenPayloadDto? Payload { get; set; }

    public static AccountTokenValidationResult Success(AccountTokenPayloadDto payload)
    {
        return new AccountTokenValidationResult
        {
            IsValid = true,
            ErrorCode = AuthErrorCode.None,
            Message = "토큰이 유효합니다.",
            Payload = payload
        };
    }

    public static AccountTokenValidationResult Fail(AuthErrorCode errorCode, string message)
    {
        return new AccountTokenValidationResult
        {
            IsValid = false,
            ErrorCode = errorCode,
            Message = message,
            Payload = null
        };
    }
}