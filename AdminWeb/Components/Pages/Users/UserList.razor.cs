using Microsoft.AspNetCore.Components;
using poscam.AdminWeb.Models.Account;
using poscam.AdminWeb.Services;

namespace poscam.AdminWeb.Components.Pages.Users;

/// <summary>
/// 담당자 목록 직접 URL 접근 시 담당자 관리 권한을 확인한다.
/// 실제 데이터 접근은 AuthServer API에서 다시 검증한다.
/// </summary>
public partial class UserList
{
    [Inject]
    private CurrentUserAccessService UserAccessService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var accessResult = await UserAccessService.GetCurrentAccessAsync();

        if (accessResult.Status == CurrentUserAccessStatus.Unauthenticated)
        {
            UserAccessService.Invalidate();
            NavigationManager.NavigateTo(
                "/login",
                forceLoad: true,
                replace: true);
            return;
        }

        if (!accessResult.Success ||
            !CurrentUserAccessPolicy.CanManagePartnerUsers(accessResult.Data))
        {
            NavigationManager.NavigateTo(
                "/users/access-denied",
                forceLoad: true,
                replace: true);
            return;
        }

        await base.OnInitializedAsync();
    }
}
