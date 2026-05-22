namespace poscam.AuthServer.Models.Enums;

/// <summary>
/// 매장 담당 역할 코드.
/// 
/// DB에는 문자열로 저장한다.
/// 역할이 추가될 가능성이 있으므로 enum보다 문자열 상수로 관리한다.
/// </summary>
public static class AssignmentRoles
{
    /// <summary>
    /// 영업/유치 담당.
    /// </summary>
    public const string Sales = "SALES";

    /// <summary>
    /// 설치 담당.
    /// </summary>
    public const string Install = "INSTALL";

    /// <summary>
    /// 관리 담당.
    /// </summary>
    public const string Manage = "MANAGE";

    /// <summary>
    /// 계약 담당.
    /// </summary>
    public const string Contract = "CONTRACT";

    /// <summary>
    /// 유지보수 담당.
    /// </summary>
    public const string Support = "SUPPORT";

    /// <summary>
    /// 기타.
    /// </summary>
    public const string Etc = "ETC";

    /// <summary>
    /// 유효한 역할 코드인지 확인한다.
    /// </summary>
    public static bool IsValid(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        return role == Sales ||
               role == Install ||
               role == Manage ||
               role == Contract ||
               role == Support ||
               role == Etc;
    }
}