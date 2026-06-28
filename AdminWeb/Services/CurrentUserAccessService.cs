using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using poscam.AdminWeb.Models.Account;
using poscam.AdminWeb.Models.Common;

namespace poscam.AdminWeb.Services;

/// <summary>
/// 현재 로그인 사용자의 역할과 관리자 세부 권한을 조회하고
/// Blazor Server의 Scoped 수명 동안 메모리에 캐시한다.
/// </summary>
public sealed class CurrentUserAccessService : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly AuthStateService _authStateService;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    private string? _cachedToken;
    private CurrentUserAccessResponse? _cachedAccess;
    private bool _disposed;

    public CurrentUserAccessService(
        HttpClient httpClient,
        AuthStateService authStateService)
    {
        _httpClient = httpClient;
        _authStateService = authStateService;
    }

    /// <summary>
    /// 현재 접근정보를 조회한다.
    /// 동일 Token의 성공 응답만 메모리에 캐시하며 실패 결과는 캐시하지 않는다.
    /// </summary>
    public async Task<CurrentUserAccessResult> GetCurrentAccessAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        string? token;

        try
        {
            token = await _authStateService.GetTokenAsync();
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException)
        {
            Invalidate();
            return CurrentUserAccessResult.Fail(
                "로그인 상태를 확인할 수 없습니다.");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            Invalidate();
            return CurrentUserAccessResult.Unauthenticated(
                "로그인이 필요합니다.");
        }

        await _cacheLock.WaitAsync(cancellationToken);

        try
        {
            if (!forceRefresh
                && string.Equals(_cachedToken, token, StringComparison.Ordinal)
                && _cachedAccess is not null)
            {
                return CurrentUserAccessResult.Ok(_cachedAccess);
            }

            if (!string.Equals(_cachedToken, token, StringComparison.Ordinal))
            {
                ClearCacheCore();
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/accounts/me/access");
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                token);

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
                return CurrentUserAccessResult.Fail(
                    "접근정보 조회 시간이 초과되었습니다.");
            }
            catch (HttpRequestException)
            {
                return CurrentUserAccessResult.Fail(
                    "접근정보 서비스를 사용할 수 없습니다.");
            }

            using (response)
            {
                var apiResponse = await ReadApiResponseAsync(
                    response,
                    cancellationToken);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    ClearCacheCore();
                    return CurrentUserAccessResult.Unauthenticated(
                        GetSafeMessage(
                            apiResponse?.Message,
                            "로그인 정보가 만료되었거나 유효하지 않습니다."),
                        apiResponse?.ErrorCode ?? 0);
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    ClearCacheCore();
                    return CurrentUserAccessResult.Forbidden(
                        GetSafeMessage(
                            apiResponse?.Message,
                            "업데이트 관리 권한이 없습니다."),
                        apiResponse?.ErrorCode ?? 0);
                }

                if (!response.IsSuccessStatusCode)
                {
                    return CurrentUserAccessResult.Fail(
                        "접근정보를 조회하는 중 오류가 발생했습니다.",
                        response.StatusCode,
                        apiResponse?.ErrorCode ?? 0);
                }

                if (apiResponse is null)
                {
                    return CurrentUserAccessResult.Fail(
                        "접근정보 응답 형식이 올바르지 않습니다.",
                        response.StatusCode);
                }

                if (!apiResponse.Success || apiResponse.Data is null)
                {
                    if (apiResponse.ErrorCode is 5001 or 5003 or 5004)
                    {
                        ClearCacheCore();
                        return CurrentUserAccessResult.Unauthenticated(
                            GetSafeMessage(
                                apiResponse.Message,
                                "로그인 정보가 만료되었거나 유효하지 않습니다."),
                            apiResponse.ErrorCode);
                    }

                    if (apiResponse.ErrorCode == 7001)
                    {
                        ClearCacheCore();
                        return CurrentUserAccessResult.Forbidden(
                            GetSafeMessage(
                                apiResponse.Message,
                                "업데이트 관리 권한이 없습니다."),
                            apiResponse.ErrorCode);
                    }

                    return CurrentUserAccessResult.Fail(
                        GetSafeMessage(
                            apiResponse.Message,
                            "접근정보를 조회하지 못했습니다."),
                        response.StatusCode,
                        apiResponse.ErrorCode);
                }

                Normalize(apiResponse.Data);
                _cachedToken = token;
                _cachedAccess = apiResponse.Data;

                return CurrentUserAccessResult.Ok(apiResponse.Data);
            }
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task<bool> CanManageUpdatesAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        var result = await GetCurrentAccessAsync(
            forceRefresh,
            cancellationToken);

        return result.Success
               && CurrentUserAccessPolicy.CanManageUpdates(result.Data);
    }

    /// <summary>
    /// 로그인·권한 변경, 401·403, 로그아웃 시 현재 캐시를 제거한다.
    /// </summary>
    public void Invalidate()
    {
        if (_disposed)
        {
            return;
        }

        ClearCacheCore();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ClearCacheCore();
        _cacheLock.Dispose();
    }

    private static async Task<ApiResponse<CurrentUserAccessResponse>?> ReadApiResponseAsync(
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

            return await JsonSerializer.DeserializeAsync<
                ApiResponse<CurrentUserAccessResponse>>(
                stream,
                JsonOptions,
                cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void Normalize(CurrentUserAccessResponse access)
    {
        access.UserName ??= "";
        access.PermissionCodes = access.PermissionCodes
            .Distinct()
            .OrderBy(code => code)
            .ToList();
    }

    private static string GetSafeMessage(
        string? message,
        string fallback)
    {
        return string.IsNullOrWhiteSpace(message)
            ? fallback
            : message.Trim();
    }

    private void ClearCacheCore()
    {
        _cachedToken = null;
        _cachedAccess = null;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
