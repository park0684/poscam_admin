using System.ComponentModel.DataAnnotations;

namespace poscam.AdminWeb.Models;

/// <summary>
/// UpdateServer 연결 주소.
/// InternalBaseUrl은 AdminWeb 서버에서 JSON API를 호출할 때 사용하고,
/// PublicBaseUrl은 C03의 browser 직접 업로드와 package URL에 사용한다.
/// </summary>
public sealed class UpdateApiSettings
{
    public const string SectionName = "UpdateApiSettings";

    [Required]
    [Url]
    public string InternalBaseUrl { get; set; } = "";

    [Required]
    [Url]
    public string PublicBaseUrl { get; set; } = "";
}
