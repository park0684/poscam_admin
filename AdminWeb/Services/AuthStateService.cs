using Microsoft.JSInterop;

namespace poscam.AdminWeb.Services;

/// <summary>
/// 관리자 웹 로그인 상태 관리 서비스.
/// 
/// 로그인 성공 시 토큰과 사용자 정보를 sessionStorage에 저장한다.
/// 이후 API 호출 시 저장된 토큰을 Authorization 헤더에 사용한다.
/// </summary>
public class AuthStateService
{
    private const string TokenKey = "poscam_admin_token";
    private const string UserCodeKey = "poscam_admin_user_code";
    private const string UserNameKey = "poscam_admin_user_name";
    private const string UserRoleKey = "poscam_admin_user_role";
    private const string PartnerCodeKey = "poscam_admin_partner_code";

    private readonly IJSRuntime _jsRuntime;

    public AuthStateService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task SaveLoginAsync(
    string token,
    int userCode,
    int? partnerCode,
    string userName,
    int userRole)
    {
        await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", TokenKey, token);
        await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", UserCodeKey, userCode.ToString());
        await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", UserNameKey, userName);
        await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", UserRoleKey, userRole.ToString());

        if (partnerCode != null)
        {
            await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", PartnerCodeKey, partnerCode.Value.ToString());
        }
        else
        {
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", PartnerCodeKey);
        }
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _jsRuntime.InvokeAsync<string?>(
            "sessionStorage.getItem",
            TokenKey);
    }

    public async Task<string?> GetUserNameAsync()
    {
        return await _jsRuntime.InvokeAsync<string?>(
            "sessionStorage.getItem",
            UserNameKey);
    }

    public async Task<int?> GetUserRoleAsync()
    {
        var value = await _jsRuntime.InvokeAsync<string?>(
            "sessionStorage.getItem",
            UserRoleKey);

        if (int.TryParse(value, out var role))
        {
            return role;
        }

        return null;
    }

    public async Task ClearAsync()
    {
        await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", TokenKey);
        await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", UserCodeKey);
        await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", UserNameKey);
        await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", UserRoleKey);
        await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", PartnerCodeKey);
    }

    public async Task<int?> GetPartnerCodeAsync()
    {
        var value = await _jsRuntime.InvokeAsync<string?>(
            "sessionStorage.getItem",
            PartnerCodeKey);

        if (int.TryParse(value, out var partnerCode))
        {
            return partnerCode;
        }

        return null;
    }

    public async Task<int?> GetUserCodeAsync()
    {
        var value = await _jsRuntime.InvokeAsync<string?>(
            "sessionStorage.getItem",
            UserCodeKey);
        if (int.TryParse(value, out var userCode))
        {
            return userCode;
        }
        return null;
    }
}