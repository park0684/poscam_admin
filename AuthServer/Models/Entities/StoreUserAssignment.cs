namespace poscam.AuthServer.Models.Entities;

/// <summary>
/// 매장 담당자 연결 Entity.
/// DB 테이블: store_user_assignments
/// 
/// 담당자가 어떤 매장을 조회/관리할 수 있는지 결정하는 핵심 테이블이다.
/// </summary>
public class StoreUserAssignment
{
    /// <summary>
    /// 연결 고유 코드.
    /// DB 컬럼: sua_code
    /// </summary>
    public int SuaCode { get; set; }

    /// <summary>
    /// 매장 코드.
    /// DB 컬럼: store_code
    /// </summary>
    public int StoreCode { get; set; }

    /// <summary>
    /// 담당자 사용자 코드.
    /// DB 컬럼: user_code
    /// </summary>
    public int UserCode { get; set; }

    /// <summary>
    /// 파트너사 코드.
    /// DB 컬럼: partner_code
    /// </summary>
    public int? PartnerCode { get; set; }

    /// <summary>
    /// 담당 역할.
    /// AssignmentRoles 상수 값 사용.
    /// 예: SALES, INSTALL, MANAGE, CONTRACT, SUPPORT, ETC
    /// DB 컬럼: assignment_role
    /// </summary>
    public string AssignmentRole { get; set; } = "";

    /// <summary>
    /// 대표 담당 여부.
    /// DB 컬럼: is_primary
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// 배정 상태.
    /// AssignmentStatus enum 값과 매칭된다.
    /// DB 컬럼: status
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// 배정한 관리자 user_code.
    /// DB 컬럼: assigned_by
    /// </summary>
    public int? AssignedBy { get; set; }

    /// <summary>
    /// 배정일시.
    /// DB 컬럼: assigned_at
    /// </summary>
    public DateTime AssignedAt { get; set; }

    /// <summary>
    /// 해제일시.
    /// DB 컬럼: released_at
    /// </summary>
    public DateTime? ReleasedAt { get; set; }
}