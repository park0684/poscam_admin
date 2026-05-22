namespace poscam.AuthServer.Models.Enums;

/// <summary>
/// 사용자 권한.
/// users.user_role 값과 매칭된다.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// 시스템 전체를 제어하는 최상위 계정.
    /// 모든 관리자 기능을 사용할 수 있으며,
    /// System 전용 삭제 API 호출이 가능하다.
    /// </summary>
    System = 0,

    /// <summary>
    /// 내부 운영 관리자.
    /// 관리자별 세부 권한은 admin_user_permissions 테이블에서 별도로 관리한다.
    /// </summary>
    Admin = 1,

    /// <summary>
    /// 본인이 배정된 매장만 조회/관리할 수 있는 파트너 담당자.
    /// 기존 담당자 계정 구분값을 그대로 유지한다.
    /// </summary>
    PartnerUser = 2
}