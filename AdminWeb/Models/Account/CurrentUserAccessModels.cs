using System.Net;

namespace poscam.AdminWeb.Models.Account;

/// <summary>
/// AuthServer의 GET /api/accounts/me/access 응답 데이터.
/// 관리자 세부 권한은 browser storage에 저장하지 않고 Scoped 메모리에서만 사용한다.
/// </summary>
public sealed class CurrentUserAccessResponse
{
    public int UserCode { get; set; }

    public string UserName { get; set; } = "";

    public int UserRole { get; set; }

    public List<int> PermissionCodes { get; set; } = new();
}

/// <summary>
/// 접근정보 API 호출 결과 상태.
/// 401과 403을 구분해 로그인 만료와 권한 부족을 다르게 처리한다.
/// </summary>
public enum CurrentUserAccessStatus
{
    Success = 0,
    Unauthenticated = 1,
    Forbidden = 2,
    Failed = 9
}

/// <summary>
/// 접근정보 조회 결과.
/// 원본 HTML이나 예외 상세 대신 화면에 사용할 안전한 메시지만 보관한다.
/// </summary>
public sealed class CurrentUserAccessResult
{
    public CurrentUserAccessStatus Status { get; init; }

    public HttpStatusCode? HttpStatusCode { get; init; }

    public int ErrorCode { get; init; }

    public string Message { get; init; } = "";

    public CurrentUserAccessResponse? Data { get; init; }

    public bool Success =>
        Status == CurrentUserAccessStatus.Success && Data is not null;

    public static CurrentUserAccessResult Ok(CurrentUserAccessResponse data)
    {
        return new CurrentUserAccessResult
        {
            Status = CurrentUserAccessStatus.Success,
            HttpStatusCode = System.Net.HttpStatusCode.OK,
            Data = data
        };
    }

    public static CurrentUserAccessResult Unauthenticated(
        string message,
        int errorCode = 0)
    {
        return new CurrentUserAccessResult
        {
            Status = CurrentUserAccessStatus.Unauthenticated,
            HttpStatusCode = System.Net.HttpStatusCode.Unauthorized,
            ErrorCode = errorCode,
            Message = message
        };
    }

    public static CurrentUserAccessResult Forbidden(
        string message,
        int errorCode = 0)
    {
        return new CurrentUserAccessResult
        {
            Status = CurrentUserAccessStatus.Forbidden,
            HttpStatusCode = System.Net.HttpStatusCode.Forbidden,
            ErrorCode = errorCode,
            Message = message
        };
    }

    public static CurrentUserAccessResult Fail(
        string message,
        HttpStatusCode? httpStatusCode = null,
        int errorCode = 0)
    {
        return new CurrentUserAccessResult
        {
            Status = CurrentUserAccessStatus.Failed,
            HttpStatusCode = httpStatusCode,
            ErrorCode = errorCode,
            Message = message
        };
    }
}
