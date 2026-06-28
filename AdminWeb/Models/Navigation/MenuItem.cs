namespace poscam.AdminWeb.Models.Navigation;

public class MenuItem
{
    public string Key { get; set; } = "";

    public string Title { get; set; } = "";

    public string? Url { get; set; }

    public int Order { get; set; }

    /// <summary>
    /// 이 메뉴를 볼 수 있는 역할 목록.
    /// 빈 목록은 기존 메뉴 정책을 유지하며 역할 제한을 적용하지 않는다.
    /// </summary>
    public List<int> Roles { get; set; } = new();

    /// <summary>
    /// 관리자 세부 권한 코드.
    /// null이면 별도 세부 권한을 요구하지 않는다.
    /// </summary>
    public int? RequiredPermissionCode { get; set; }

    public List<MenuItem> Children { get; set; } = new();

    public bool HasChildren => Children.Count > 0;
}
