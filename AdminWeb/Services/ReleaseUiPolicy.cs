using poscam.AdminWeb.Models.Updates;

namespace poscam.AdminWeb.Services;

/// <summary>
/// 릴리스 상태별 화면 동작 정책.
/// Component와 테스트가 동일한 규칙을 사용하도록 분리한다.
/// </summary>
public static class ReleaseUiPolicy
{
    public static bool CanEdit(int status)
    {
        return status == ReleaseStatusCodes.Draft;
    }

    public static bool CanDelete(int status)
    {
        return status == ReleaseStatusCodes.Draft;
    }

    public static bool CanPublish(int status, int artifactCount)
    {
        return status == ReleaseStatusCodes.Draft && artifactCount > 0;
    }

    public static bool CanDisable(int status)
    {
        return status == ReleaseStatusCodes.Published;
    }

    public static bool IsReadOnly(int status)
    {
        return status is ReleaseStatusCodes.Published
            or ReleaseStatusCodes.Disabled;
    }
}
