using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace poscam.AdminWeb.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AuthStateService _authStateService;

    public ApiClient(
        HttpClient httpClient,
        AuthStateService authStateService)
    {
        _httpClient = httpClient;
        _authStateService = authStateService;
    }

    public async Task<TResponse?> GetAsync<TResponse>(
        string url,
        bool withAuth = true)
    {
        await SetAuthHeaderAsync(withAuth);

        var response = await _httpClient.GetAsync(url);
        return await ReadJsonResponseAsync<TResponse>(response);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string url,
        TRequest request,
        bool withAuth = true)
    {
        await SetAuthHeaderAsync(withAuth);

        var response = await _httpClient.PostAsJsonAsync(url, request);
        return await ReadJsonResponseAsync<TResponse>(response);
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(
        string url,
        TRequest request,
        bool withAuth = true)
    {
        await SetAuthHeaderAsync(withAuth);

        var response = await _httpClient.PutAsJsonAsync(url, request);
        return await ReadJsonResponseAsync<TResponse>(response);
    }

    public async Task<TResponse?> DeleteAsync<TResponse>(
        string url,
        bool withAuth = true)
    {
        await SetAuthHeaderAsync(withAuth);

        var response = await _httpClient.DeleteAsync(url);
        return await ReadJsonResponseAsync<TResponse>(response);
    }

    private async Task<TResponse?> ReadJsonResponseAsync<TResponse>(
        HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException(
                $"서버 응답이 비어 있습니다. StatusCode: {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        try
        {
            return JsonSerializer.Deserialize<TResponse>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch (JsonException)
        {
            throw new InvalidOperationException(
                $"서버가 JSON이 아닌 응답을 반환했습니다. StatusCode: {(int)response.StatusCode} {response.ReasonPhrase}, Response: {content}");
        }
    }

    private async Task SetAuthHeaderAsync(bool withAuth)
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;

        if (!withAuth)
        {
            return;
        }

        var token = await _authStateService.GetTokenAsync();

        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }
}