namespace poscam.AuthServer.Models.Dtos.Store;

/// <summary>
/// 매장 담당자 연결 요청 DTO.
/// 
/// 특정 매장에 담당자와 역할을 연결한다.
/// </summary>
public class StoreAssignmentCreateRequest
{
    /// <summary>
    /// 매장 코드.
    /// </summary>
    public int StoreCode { get; set; }

    /// <summary>
    /// 담당자 사용자 코드.
    /// </summary>
    public int UserCode { get; set; }

    /// <summary>
    /// 파트너사 코드.
    /// 사용자의 소속 파트너사를 기본값으로 사용할 수 있다.
    /// </summary>
    public int? PartnerCode { get; set; }

    /// <summary>
    /// 담당 역할.
    /// SALES, INSTALL, MANAGE, CONTRACT, SUPPORT, ETC
    /// </summary>
    public string AssignmentRole { get; set; } = "";

    /// <summary>
    /// 대표 담당 여부.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// 배정한 관리자 코드.
    /// 추후 로그인 토큰에서 가져오는 구조로 변경 가능.
    /// </summary>
    public int? AssignedBy { get; set; }
}