namespace poscam.AdminWeb.Models.Store;

/// <summary>
/// 매장 담당자 연결 응답 DTO.
/// </summary>
public class StoreAssignmentResponse
{
    public int AssignmentCode { get; set; }

    public int StoreCode { get; set; }

    public int UserCode { get; set; }

    public string AssignmentRole { get; set; } = "";

    public bool Saved { get; set; }
}