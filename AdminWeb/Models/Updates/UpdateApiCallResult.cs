using System.Net;

namespace poscam.AdminWeb.Models.Updates;

/// <summary>
/// UpdateServer 호출 결과. HTTP 상태와 업무 오류 코드를 함께 보존한다.
/// </summary>
public sealed class UpdateApiCallResult<T>
{
    public bool Success { get; init; }

    public HttpStatusCode? StatusCode { get; init; }

    public int ErrorCode { get; init; }

    public string Message { get; init; } = "";

    public string? RequestId { get; init; }

    public T? Data { get; init; }

    public bool IsUnauthorized =>
        StatusCode == HttpStatusCode.Unauthorized
        || ErrorCode is 5001 or 5003 or 5004;

    public bool IsForbidden =>
        StatusCode == HttpStatusCode.Forbidden
        || ErrorCode == 7001;

    public bool IsNotFound =>
        StatusCode == HttpStatusCode.NotFound
        || ErrorCode is 8010 or 8020;

    public bool IsConflict =>
        StatusCode == HttpStatusCode.Conflict
        || ErrorCode is 8011 or 8012 or 8022 or 8033;

    public bool IsServiceUnavailable =>
        StatusCode == HttpStatusCode.ServiceUnavailable
        || ErrorCode == 9003;

    public static UpdateApiCallResult<T> Ok(
        T data,
        string? message,
        HttpStatusCode statusCode,
        string? requestId)
    {
        return new UpdateApiCallResult<T>
        {
            Success = true,
            StatusCode = statusCode,
            Message = message?.Trim() ?? "",
            RequestId = requestId,
            Data = data
        };
    }

    public static UpdateApiCallResult<T> Fail(
        string message,
        HttpStatusCode? statusCode = null,
        int errorCode = 0,
        string? requestId = null)
    {
        return new UpdateApiCallResult<T>
        {
            Success = false,
            StatusCode = statusCode,
            ErrorCode = errorCode,
            Message = message,
            RequestId = requestId
        };
    }
}
