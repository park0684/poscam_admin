using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using poscam.AdminWeb.Models.Common;
using poscam.AdminWeb.Models.Updates;

namespace poscam.AdminWeb.Services;

/// <summary>
/// UpdateServer 관리자 JSON API 전용 Client.
/// 기존 AuthServer ApiClient와 BaseAddress·오류 계약을 공유하지 않는다.
/// </summary>
public sealed class UpdateApiClient
{
    private const string RequestIdHeaderName = "X-Request-ID";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly AuthStateService _authStateService;

    public UpdateApiClient(
        HttpClient httpClient,
        AuthStateService authStateService)
    {
        _httpClient = httpClient;
        _authStateService = authStateService;
    }

    public Task<UpdateApiCallResult<TResponse>> GetAsync<TResponse>(
        string relativeUrl,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<object, TResponse>(
            HttpMethod.Get,
            relativeUrl,
            requestBody: null,
            cancellationToken);
    }

    public Task<UpdateApiCallResult<TResponse>> PostAsync<TRequest, TResponse>(
        string relativeUrl,
        TRequest? requestBody,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<TRequest, TResponse>(
            HttpMethod.Post,
            relativeUrl,
            requestBody,
            cancellationToken);
    }

    public Task<UpdateApiCallResult<TResponse>> PostAsync<TResponse>(
        string relativeUrl,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<object, TResponse>(
            HttpMethod.Post,
            relativeUrl,
            requestBody: null,
            cancellationToken);
    }

    public Task<UpdateApiCallResult<TResponse>> PutAsync<TRequest, TResponse>(
        string relativeUrl,
        TRequest requestBody,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<TRequest, TResponse>(
            HttpMethod.Put,
            relativeUrl,
            requestBody,
            cancellationToken);
    }

    public Task<UpdateApiCallResult<TResponse>> DeleteAsync<TResponse>(
        string relativeUrl,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<object, TResponse>(
            HttpMethod.Delete,
            relativeUrl,
            requestBody: null,
            cancellationToken);
    }

    private async Task<UpdateApiCallResult<TResponse>> SendAsync<TRequest, TResponse>(
        HttpMethod method,
        string relativeUrl,
        TRequest? requestBody,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl))
        {
            return UpdateApiCallResult<TResponse>.Fail(
                "UpdateServer 요청 주소가 올바르지 않습니다.");
        }

        string? token;

        try
        {
            token = await _authStateService.GetTokenAsync();
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException)
        {
            return UpdateApiCallResult<TResponse>.Fail(
                "로그인 상태를 확인할 수 없습니다.");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return UpdateApiCallResult<TResponse>.Fail(
                "로그인이 필요합니다.",
                HttpStatusCode.Unauthorized,
                errorCode: 5001);
        }

        using var request = new HttpRequestMessage(method, relativeUrl.TrimStart('/'));
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token);
        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        if (requestBody is not null)
        {
            request.Content = JsonContent.Create(
                requestBody,
                options: JsonOptions);
        }

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return UpdateApiCallResult<TResponse>.Fail(
                "UpdateServer 응답 시간이 초과되었습니다.");
        }
        catch (HttpRequestException)
        {
            return UpdateApiCallResult<TResponse>.Fail(
                "UpdateServer에 연결할 수 없습니다.");
        }

        using (response)
        {
            var requestId = TryGetRequestId(response);
            var apiResponse = await ReadApiResponseAsync<TResponse>(
                response,
                cancellationToken);

            if (apiResponse is null)
            {
                return UpdateApiCallResult<TResponse>.Fail(
                    response.IsSuccessStatusCode
                        ? "UpdateServer 응답 형식이 올바르지 않습니다."
                        : GetFallbackMessage(response.StatusCode),
                    response.StatusCode,
                    requestId: requestId);
            }

            if (!response.IsSuccessStatusCode
                || !apiResponse.Success
                || apiResponse.Data is null)
            {
                return UpdateApiCallResult<TResponse>.Fail(
                    GetSafeMessage(
                        apiResponse.Message,
                        GetFallbackMessage(response.StatusCode)),
                    response.StatusCode,
                    apiResponse.ErrorCode,
                    requestId);
            }

            return UpdateApiCallResult<TResponse>.Ok(
                apiResponse.Data,
                apiResponse.Message,
                response.StatusCode,
                requestId);
        }
    }

    private static async Task<ApiResponse<T>?> ReadApiResponseAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength == 0)
        {
            return null;
        }

        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(
                cancellationToken);

            if (stream.CanSeek && stream.Length == 0)
            {
                return null;
            }

            return await JsonSerializer.DeserializeAsync<ApiResponse<T>>(
                stream,
                JsonOptions,
                cancellationToken);
        }
        catch (Exception exception)
            when (exception is JsonException
                  or IOException
                  or HttpRequestException
                  or NotSupportedException)
        {
            return null;
        }
    }

    private static string? TryGetRequestId(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues(
                RequestIdHeaderName,
                out var values)
            ? values.FirstOrDefault()
            : null;
    }

    private static string GetSafeMessage(
        string? message,
        string fallback)
    {
        return string.IsNullOrWhiteSpace(message)
            ? fallback
            : message.Trim();
    }

    private static string GetFallbackMessage(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => "입력값을 확인해 주세요.",
            HttpStatusCode.Unauthorized => "로그인이 만료되었거나 유효하지 않습니다.",
            HttpStatusCode.Forbidden => "업데이트 관리 권한이 없습니다.",
            HttpStatusCode.NotFound => "요청한 릴리스를 찾을 수 없습니다.",
            HttpStatusCode.Conflict => "현재 상태에서는 요청한 작업을 수행할 수 없습니다.",
            HttpStatusCode.ServiceUnavailable => "관리자 권한 확인 서비스를 사용할 수 없습니다.",
            _ => "UpdateServer 요청을 처리하지 못했습니다."
        };
    }
}
