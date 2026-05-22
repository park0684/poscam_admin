namespace poscam.AdminWeb.Models.Navigation;

public class MenuItem
{
    public string Key { get; set; } = "";

    public string Title { get; set; } = "";

    public string? Url { get; set; }

    public int Order { get; set; }

    public List<int> Roles { get; set; } = new();

    public List<MenuItem> Children { get; set; } = new();

    public bool HasChildren => Children.Count > 0;
}