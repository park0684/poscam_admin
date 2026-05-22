namespace poscam.AdminWeb.Models.Store;

/// <summary>
/// 매장 담당자 연결 요청 DTO.
/// </summary>
public class StoreAssignmentCreateRequest
{
    public int StoreCode { get; set; }

    public int UserCode { get; set; }

    public int? PartnerCode { get; set; }

    public string AssignmentRole { get; set; } = "MANAGE";

    public bool IsPrimary { get; set; }

    public int? AssignedBy { get; set; }
}