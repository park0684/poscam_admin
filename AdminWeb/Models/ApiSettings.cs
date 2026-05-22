namespace poscam.AdminWeb.Models;

/// <summary>
/// 백엔드 API 접속 설정.
/// appsettings.json의 ApiSettings 섹션과 매핑된다.
/// </summary>
public class ApiSettings
{
    public string BaseUrl { get; set; } = "";
}