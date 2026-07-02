namespace poscam.AdminWeb.Components.Pages.AdminAccounts;

public partial class AdminAccountDetailPopup
{
    public AdminAccountDetailPopup()
    {
        _permissions.Add(
            new PermissionViewItem(
                13,
                "장비 관리",
                "PC캠 및 캠뷰어 장비 초기화"));
    }
}
