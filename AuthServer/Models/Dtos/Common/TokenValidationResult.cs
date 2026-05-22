using poscam.AuthServer.Models.Enums;

namespace poscam.AuthServer.Models.Dtos.Common;

/// <summary>
/// 토큰 검증 결과 DTO.
/// 
/// TokenService.ValidateToken()의 반환값으로 사용한다.
/// </summary>
public class TokenValidationResult
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
    /// 실패 또는 성공 메시지.
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// 토큰 검증 성공 시 복원된 Payload.
    /// </summary>
    public AuthTokenPayloadDto? Payload { get; set; }

    public static TokenValidationResult Success(AuthTokenPayloadDto payload)
    {
        return new TokenValidationResult
        {
            IsValid = true,
            ErrorCode = AuthErrorCode.None,
            Message = "토큰이 유효합니다.",
            Payload = payload
        };
    }

    public static TokenValidationResult Fail(AuthErrorCode errorCode, string message)
    {
        return new TokenValidationResult
        {
            IsValid = false,
            ErrorCode = errorCode,
            Message = message,
            Payload = null
        };
    }
}