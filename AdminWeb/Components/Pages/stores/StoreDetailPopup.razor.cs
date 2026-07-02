using Microsoft.AspNetCore.Components;
using poscam.AdminWeb.Models.Account;
using poscam.AdminWeb.Services;

namespace poscam.AdminWeb.Components.Pages.stores;

/// <summary>
/// 매장 상세/등록 팝업의 직접 URL 접근 권한을 확인한다.
/// 실제 저장과 조회 보안 경계는 AuthServer API에서 다시 검증한다.
/// </summary>
public partial class StoreDetailPopup
{
    [Inject]
    private CurrentUserAccessService StoreAccessService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var accessResult = await StoreAccessService.GetCurrentAccessAsync();

        if (accessResult.Status == CurrentUserAccessStatus.Unauthenticated)
        {
            StoreAccessService.Invalidate();
            NavigationManager.NavigateTo(
                "/login",
                forceLoad: true,
                replace: true);
            return;
        }

        if (!accessResult.Success ||
            !CurrentUserAccessPolicy.CanManageStores(accessResult.Data))
        {
            NavigationManager.NavigateTo(
                "/stores/popup/access-denied",
                forceLoad: true,
                replace: true);
            return;
        }

        await base.OnInitializedAsync();
    }
}
